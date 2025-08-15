using Microsoft.Extensions.Logging;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Infrastructure.Services;

public class DefaultTaskProcessor : ITaskProcessor
{
    private readonly ILogger<DefaultTaskProcessor> _logger;

    public DefaultTaskProcessor(ILogger<DefaultTaskProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<TaskExecutionResult> ProcessTaskAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing task {TaskId} of type {TaskType}", task.Id, task.Type);

            var startTime = DateTime.UtcNow;

            // Simulate task processing based on payload
            if (!string.IsNullOrEmpty(task.Payload))
            {
                // Try to parse payload as JSON for different task types
                try
                {
                    var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(task.Payload);
                    
                    // Handle different task types
                    switch (task.Type)
                    {
                        case Core.Enums.TaskType.Immediate:
                            await ProcessImmediateTask(payload, cancellationToken);
                            break;
                        case Core.Enums.TaskType.Scheduled:
                            await ProcessScheduledTask(payload, cancellationToken);
                            break;
                        case Core.Enums.TaskType.Delayed:
                            await ProcessDelayedTask(payload, cancellationToken);
                            break;
                        case Core.Enums.TaskType.Recurring:
                            await ProcessRecurringTask(payload, cancellationToken);
                            break;
                        default:
                            await ProcessDefaultTask(payload, cancellationToken);
                            break;
                    }
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    // If payload is not JSON, treat as simple text task
                    await ProcessTextTask(task.Payload, cancellationToken);
                }
            }
            else
            {
                // Empty payload - just simulate some work
                await Task.Delay(1000, cancellationToken);
            }

            var executionTime = DateTime.UtcNow - startTime;

            return new TaskExecutionResult
            {
                IsSuccess = true,
                Result = $"Task completed successfully in {executionTime.TotalMilliseconds:F2}ms",
                ExecutionTime = executionTime,
                ShouldRetry = false
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Task {TaskId} was cancelled", task.Id);
            return new TaskExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = "Task was cancelled",
                ShouldRetry = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing task {TaskId}", task.Id);
            return new TaskExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ShouldRetry = true
            };
        }
    }

    public bool CanHandle(string taskType)
    {
        // This default processor can handle all task types
        return true;
    }

    private async Task ProcessImmediateTask(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing immediate task with payload: {Payload}", payload?.ToString());
        await Task.Delay(500, cancellationToken); // Simulate quick processing
    }

    private async Task ProcessScheduledTask(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing scheduled task with payload: {Payload}", payload?.ToString());
        await Task.Delay(1000, cancellationToken); // Simulate processing
    }

    private async Task ProcessDelayedTask(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing delayed task with payload: {Payload}", payload?.ToString());
        await Task.Delay(1500, cancellationToken); // Simulate longer processing
    }

    private async Task ProcessRecurringTask(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing recurring task with payload: {Payload}", payload?.ToString());
        await Task.Delay(2000, cancellationToken); // Simulate complex processing
    }

    private async Task ProcessDefaultTask(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing default task with payload: {Payload}", payload?.ToString());
        await Task.Delay(1000, cancellationToken);
    }

    private async Task ProcessTextTask(string payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing text task: {Payload}", payload);
        await Task.Delay(800, cancellationToken);
    }
}