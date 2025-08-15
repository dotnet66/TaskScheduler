using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using System.Text;

namespace RabbitMQTaskScheduler.Infrastructure.RabbitMQ;

public class RabbitMQTaskProducer : ITaskProducer, IDisposable
{
    private readonly RabbitMQConfiguration _config;
    private readonly ILogger<RabbitMQTaskProducer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQTaskProducer(IOptions<RabbitMQConfiguration> config, ILogger<RabbitMQTaskProducer> logger)
    {
        _config = config.Value;
        _logger = logger;

        var factory = new ConnectionFactory()
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange and queues
        DeclareExchangesAndQueues();
    }

    private void DeclareExchangesAndQueues()
    {
        // Main exchange
        _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct, _config.Durable);

        // Main task queue
        _channel.QueueDeclare(_config.QueueName, _config.Durable, false, false, null);
        _channel.QueueBind(_config.QueueName, _config.ExchangeName, "task");

        // Delayed task queue (for delayed message plugin or TTL approach)
        if (_config.DelayedQueueName != _config.QueueName)
        {
            _channel.QueueDeclare(_config.DelayedQueueName, _config.Durable, false, false, null);
            _channel.QueueBind(_config.DelayedQueueName, _config.ExchangeName, "delayed");
        }
    }

    public async Task<bool> PublishTaskAsync(TaskItem task)
    {
        try
        {
            var message = JsonConvert.SerializeObject(task);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = _config.Durable;
            properties.Priority = (byte)task.Priority;

            _channel.BasicPublish(_config.ExchangeName, "task", properties, body);

            _logger.LogInformation("Published task {TaskId} to queue", task.Id);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish task {TaskId}", task.Id);
            return false;
        }
    }

    public async Task<bool> PublishDelayedTaskAsync(TaskItem task, TimeSpan delay)
    {
        try
        {
            var message = JsonConvert.SerializeObject(task);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = _config.Durable;
            properties.Priority = (byte)task.Priority;
            
            // Set TTL for delayed execution
            properties.Expiration = ((int)delay.TotalMilliseconds).ToString();

            // Use delayed queue with TTL and dead letter exchange
            var delayedQueueName = $"{_config.QueueName}-delayed-{delay.TotalSeconds}s";
            
            // Declare temporary queue for delay
            var queueArgs = new Dictionary<string, object>
            {
                { "x-message-ttl", (int)delay.TotalMilliseconds },
                { "x-dead-letter-exchange", _config.ExchangeName },
                { "x-dead-letter-routing-key", "task" }
            };

            _channel.QueueDeclare(delayedQueueName, false, false, true, queueArgs);
            _channel.BasicPublish("", delayedQueueName, properties, body);

            _logger.LogInformation("Published delayed task {TaskId} with delay {Delay}", task.Id, delay);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish delayed task {TaskId}", task.Id);
            return false;
        }
    }

    public async Task<bool> PublishScheduledTaskAsync(TaskItem task, DateTime scheduledTime)
    {
        var delay = scheduledTime - DateTime.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return await PublishTaskAsync(task);
        }

        return await PublishDelayedTaskAsync(task, delay);
    }

    public async Task<bool> PublishBatchTasksAsync(IEnumerable<TaskItem> tasks)
    {
        try
        {
            var batch = _channel.CreateBasicPublishBatch();

            foreach (var task in tasks)
            {
                var message = JsonConvert.SerializeObject(task);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = _config.Durable;
                properties.Priority = (byte)task.Priority;

                batch.Add(_config.ExchangeName, "task", false, properties, body.AsMemory());
            }

            batch.Publish();

            _logger.LogInformation("Published batch of {Count} tasks", tasks.Count());
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch tasks");
            return false;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}