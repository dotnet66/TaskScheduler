using Microsoft.EntityFrameworkCore;
using RabbitMQTaskScheduler.Core.Enums;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using RabbitMQTaskScheduler.Infrastructure.Data;

namespace RabbitMQTaskScheduler.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskSchedulerDbContext _context;

    public TaskRepository(TaskSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(string id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskItemStatus status)
    {
        return await _context.Tasks
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetPendingTasksAsync()
    {
        return await _context.Tasks
            .Where(t => t.Status == TaskItemStatus.Pending)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetScheduledTasksAsync(DateTime before)
    {
        return await _context.Tasks
            .Where(t => t.Status == TaskItemStatus.Pending && 
                       t.ScheduledAt.HasValue && 
                       t.ScheduledAt.Value <= before)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.ScheduledAt)
            .ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(string id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TaskStatistics> GetStatisticsAsync()
    {
        var totalTasks = await _context.Tasks.CountAsync();
        var pendingTasks = await _context.Tasks.CountAsync(t => t.Status == TaskItemStatus.Pending);
        var processingTasks = await _context.Tasks.CountAsync(t => t.Status == TaskItemStatus.Processing);
        var completedTasks = await _context.Tasks.CountAsync(t => t.Status == TaskItemStatus.Completed);
        var failedTasks = await _context.Tasks.CountAsync(t => t.Status == TaskItemStatus.Failed);
        var activeConsumers = await _context.Consumers.CountAsync(c => c.IsActive);

        var completedTasksWithTime = await _context.Tasks
            .Where(t => t.Status == TaskItemStatus.Completed && t.ExecutionDuration.HasValue)
            .Select(t => t.ExecutionDuration!.Value.TotalMilliseconds)
            .ToListAsync();

        var averageExecutionTime = completedTasksWithTime.Any() ? completedTasksWithTime.Average() : 0;
        var successRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;

        return new TaskStatistics
        {
            TotalTasks = totalTasks,
            PendingTasks = pendingTasks,
            ProcessingTasks = processingTasks,
            CompletedTasks = completedTasks,
            FailedTasks = failedTasks,
            ActiveConsumers = activeConsumers,
            AverageExecutionTime = averageExecutionTime,
            SuccessRate = successRate
        };
    }

    public async Task<IEnumerable<TaskItem>> GetTasksPagedAsync(int page, int pageSize, string? searchTerm = null, TaskItemStatus? status = null)
    {
        var query = _context.Tasks.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(t => t.Name.Contains(searchTerm) || t.Description.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}