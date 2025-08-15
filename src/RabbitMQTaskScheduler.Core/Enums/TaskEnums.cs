namespace RabbitMQTaskScheduler.Core.Enums;

public enum TaskType
{
    Immediate = 0,
    Scheduled = 1,
    Delayed = 2,
    Recurring = 3
}

public enum TaskItemStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Retrying = 5
}

public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
