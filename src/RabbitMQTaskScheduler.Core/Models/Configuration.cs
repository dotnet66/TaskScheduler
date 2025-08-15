namespace RabbitMQTaskScheduler.Core.Models;

public class RabbitMQConfiguration
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = "task-queue";
    public string ExchangeName { get; set; } = "task-exchange";
    public string DelayedQueueName { get; set; } = "delayed-task-queue";
    public bool Durable { get; set; } = true;
    public int PrefetchCount { get; set; } = 10;
}

public class TaskSchedulerConfiguration
{
    public int DefaultConsumerCount { get; set; } = 3;
    public int MaxConsumerCount { get; set; } = 10;
    public int MinConsumerCount { get; set; } = 1;
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
    public int ConsumerHeartbeatIntervalSeconds { get; set; } = 30;
    public int ConsumerTimeoutSeconds { get; set; } = 300;
    public bool EnableDelayedTasks { get; set; } = true;
    public int StatisticsUpdateIntervalSeconds { get; set; } = 60;
}

public class RetryPolicyConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public int BackoffMultiplier { get; set; } = 2;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
}