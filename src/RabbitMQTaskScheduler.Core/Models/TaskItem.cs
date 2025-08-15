using RabbitMQTaskScheduler.Core.Enums;

namespace RabbitMQTaskScheduler.Core.Models;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskType Type { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan? ExecutionDuration => StartedAt.HasValue && CompletedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : null;
    
    // Additional properties for scheduling
    public string? CronExpression { get; set; } // For recurring tasks
    public TimeSpan? DelayTime { get; set; } // For delayed tasks
    public string? AssignedConsumer { get; set; } // Track which consumer is processing
    public Dictionary<string, string> Metadata { get; set; } = new();
}