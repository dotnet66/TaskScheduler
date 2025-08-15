namespace RabbitMQTaskScheduler.Core.Models;

public class ConsumerInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public int ProcessedTasksCount { get; set; }
    public int FailedTasksCount { get; set; }
    public DateTime StartedAt { get; set; }
    public string? CurrentTaskId { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public int ProcessId { get; set; } = Environment.ProcessId;
}

public class TaskStatistics
{
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int ProcessingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int ActiveConsumers { get; set; }
    public double AverageExecutionTime { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class TaskExecutionResult
{
    public bool IsSuccess { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool ShouldRetry { get; set; }
}