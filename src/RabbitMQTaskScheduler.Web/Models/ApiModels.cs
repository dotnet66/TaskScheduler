using RabbitMQTaskScheduler.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace RabbitMQTaskScheduler.Web.Models;

public class CreateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public TaskType Type { get; set; } = TaskType.Immediate;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public string Payload { get; set; } = string.Empty;

    public DateTime? ScheduledAt { get; set; }

    public TimeSpan? DelayTime { get; set; }

    public string? CronExpression { get; set; }

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public Dictionary<string, string>? Metadata { get; set; }
}

public class UpdateTaskRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public TaskPriority? Priority { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public class TaskQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public TaskItemStatus? Status { get; set; }
}

public class ScaleConsumersRequest
{
    [Range(1, 50)]
    public int TargetCount { get; set; }
}