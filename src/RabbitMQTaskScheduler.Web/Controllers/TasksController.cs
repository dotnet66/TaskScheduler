using Microsoft.AspNetCore.Mvc;
using RabbitMQTaskScheduler.Core.Enums;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using RabbitMQTaskScheduler.Web.Models;

namespace RabbitMQTaskScheduler.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskProducer _taskProducer;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskRepository taskRepository,
        ITaskProducer taskProducer,
        ILogger<TasksController> logger)
    {
        _taskRepository = taskRepository;
        _taskProducer = taskProducer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] TaskItemStatus? status = null)
    {
        try
        {
            var tasks = await _taskRepository.GetTasksPagedAsync(page, pageSize, search, status);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTask(string id)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            var task = new TaskItem
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Priority = request.Priority,
                Payload = request.Payload,
                ScheduledAt = request.ScheduledAt,
                DelayTime = request.DelayTime,
                CronExpression = request.CronExpression,
                MaxRetries = request.MaxRetries,
                Status = TaskItemStatus.Pending,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Save to database first
            var createdTask = await _taskRepository.CreateAsync(task);

            // Publish to queue
            bool published;
            if (task.Type == TaskType.Delayed && task.DelayTime.HasValue)
            {
                published = await _taskProducer.PublishDelayedTaskAsync(task, task.DelayTime.Value);
            }
            else if (task.Type == TaskType.Scheduled && task.ScheduledAt.HasValue)
            {
                published = await _taskProducer.PublishScheduledTaskAsync(task, task.ScheduledAt.Value);
            }
            else
            {
                published = await _taskProducer.PublishTaskAsync(task);
            }

            if (!published)
            {
                _logger.LogWarning("Failed to publish task {TaskId} to queue", task.Id);
            }

            return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<IEnumerable<TaskItem>>> CreateBatchTasks([FromBody] IEnumerable<CreateTaskRequest> requests)
    {
        try
        {
            var tasks = new List<TaskItem>();
            
            foreach (var request in requests)
            {
                var task = new TaskItem
                {
                    Name = request.Name,
                    Description = request.Description,
                    Type = request.Type,
                    Priority = request.Priority,
                    Payload = request.Payload,
                    ScheduledAt = request.ScheduledAt,
                    DelayTime = request.DelayTime,
                    CronExpression = request.CronExpression,
                    MaxRetries = request.MaxRetries,
                    Status = TaskItemStatus.Pending,
                    Metadata = request.Metadata ?? new Dictionary<string, string>()
                };

                tasks.Add(await _taskRepository.CreateAsync(task));
            }

            // Publish batch to queue
            var published = await _taskProducer.PublishBatchTasksAsync(tasks);
            if (!published)
            {
                _logger.LogWarning("Failed to publish batch tasks to queue");
            }

            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch tasks");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItem>> UpdateTask(string id, [FromBody] UpdateTaskRequest request)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Only allow certain fields to be updated
            task.Name = request.Name ?? task.Name;
            task.Description = request.Description ?? task.Description;
            task.Priority = request.Priority ?? task.Priority;
            
            if (request.Metadata != null)
            {
                task.Metadata = request.Metadata;
            }

            var updatedTask = await _taskRepository.UpdateAsync(task);
            return Ok(updatedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTask(string id)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            await _taskRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<TaskStatistics>> GetStatistics()
    {
        try
        {
            var statistics = await _taskRepository.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}