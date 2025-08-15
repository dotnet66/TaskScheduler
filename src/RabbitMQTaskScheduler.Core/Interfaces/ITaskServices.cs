using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Core.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(string id);
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> GetByStatusAsync(Enums.TaskItemStatus status);
    Task<IEnumerable<TaskItem>> GetPendingTasksAsync();
    Task<IEnumerable<TaskItem>> GetScheduledTasksAsync(DateTime before);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<TaskItem> UpdateAsync(TaskItem task);
    Task DeleteAsync(string id);
    Task<TaskStatistics> GetStatisticsAsync();
    Task<IEnumerable<TaskItem>> GetTasksPagedAsync(int page, int pageSize, string? searchTerm = null, Enums.TaskItemStatus? status = null);
}

public interface ITaskProducer
{
    Task<bool> PublishTaskAsync(TaskItem task);
    Task<bool> PublishDelayedTaskAsync(TaskItem task, TimeSpan delay);
    Task<bool> PublishScheduledTaskAsync(TaskItem task, DateTime scheduledTime);
    Task<bool> PublishBatchTasksAsync(IEnumerable<TaskItem> tasks);
}

public interface ITaskConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    string ConsumerId { get; }
    event EventHandler<TaskItem> TaskProcessed;
    event EventHandler<(TaskItem Task, Exception Exception)> TaskFailed;
}

public interface ITaskProcessor
{
    Task<TaskExecutionResult> ProcessTaskAsync(TaskItem task, CancellationToken cancellationToken = default);
    bool CanHandle(string taskType);
}

public interface IConsumerManager
{
    Task<IEnumerable<ConsumerInfo>> GetActiveConsumersAsync();
    Task<ConsumerInfo> RegisterConsumerAsync(string consumerId);
    Task UnregisterConsumerAsync(string consumerId);
    Task UpdateConsumerHeartbeatAsync(string consumerId);
    Task<int> GetActiveConsumerCountAsync();
    Task ScaleConsumersAsync(int targetCount);
}