using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using System.Text;

namespace RabbitMQTaskScheduler.Infrastructure.RabbitMQ;

public class RabbitMQTaskConsumer : ITaskConsumer, IDisposable
{
    private readonly RabbitMQConfiguration _config;
    private readonly ILogger<RabbitMQTaskConsumer> _logger;
    private readonly ITaskProcessor _taskProcessor;
    private readonly ITaskRepository _taskRepository;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _consumerTag;
    private bool _disposed = false;

    public string ConsumerId { get; } = Guid.NewGuid().ToString();
    public bool IsRunning { get; private set; }

    public event EventHandler<TaskItem>? TaskProcessed;
    public event EventHandler<(TaskItem Task, Exception Exception)>? TaskFailed;

    public RabbitMQTaskConsumer(
        IOptions<RabbitMQConfiguration> config,
        ILogger<RabbitMQTaskConsumer> logger,
        ITaskProcessor taskProcessor,
        ITaskRepository taskRepository)
    {
        _config = config.Value;
        _logger = logger;
        _taskProcessor = taskProcessor;
        _taskRepository = taskRepository;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return;

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Set QoS for fair dispatching
            _channel.BasicQos(0, (ushort)_config.PrefetchCount, false);

            // Ensure queue exists
            _channel.QueueDeclare(_config.QueueName, _config.Durable, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;

            _consumerTag = _channel.BasicConsume(_config.QueueName, false, ConsumerId, consumer);
            IsRunning = true;

            _logger.LogInformation("Consumer {ConsumerId} started", ConsumerId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consumer {ConsumerId}", ConsumerId);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return;

        try
        {
            if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
            {
                _channel.BasicCancel(_consumerTag);
            }

            IsRunning = false;
            _logger.LogInformation("Consumer {ConsumerId} stopped", ConsumerId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping consumer {ConsumerId}", ConsumerId);
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        TaskItem? task = null;
        try
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            task = JsonConvert.DeserializeObject<TaskItem>(message);

            if (task == null)
            {
                _logger.LogWarning("Failed to deserialize task message");
                _channel?.BasicNack(eventArgs.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation("Processing task {TaskId} by consumer {ConsumerId}", task.Id, ConsumerId);

            // Update task status to processing
            task.Status = Core.Enums.TaskItemStatus.Processing;
            task.StartedAt = DateTime.UtcNow;
            task.AssignedConsumer = ConsumerId;
            await _taskRepository.UpdateAsync(task);

            // Process the task
            var result = await _taskProcessor.ProcessTaskAsync(task);

            if (result.IsSuccess)
            {
                task.Status = Core.Enums.TaskItemStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.Result = result.Result;
                await _taskRepository.UpdateAsync(task);

                _channel?.BasicAck(eventArgs.DeliveryTag, false);
                TaskProcessed?.Invoke(this, task);

                _logger.LogInformation("Task {TaskId} completed successfully", task.Id);
            }
            else
            {
                await HandleTaskFailure(task, eventArgs, new Exception(result.ErrorMessage ?? "Task processing failed"), result.ShouldRetry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing task {TaskId}", task?.Id ?? "unknown");
            
            if (task != null)
            {
                await HandleTaskFailure(task, eventArgs, ex, true);
            }
            else
            {
                _channel?.BasicNack(eventArgs.DeliveryTag, false, false);
            }
        }
    }

    private async Task HandleTaskFailure(TaskItem task, BasicDeliverEventArgs eventArgs, Exception exception, bool shouldRetry)
    {
        task.RetryCount++;
        task.ErrorMessage = exception.Message;

        if (shouldRetry && task.RetryCount < task.MaxRetries)
        {
            task.Status = Core.Enums.TaskItemStatus.Retrying;
            await _taskRepository.UpdateAsync(task);

            // Requeue the message for retry
            _channel?.BasicNack(eventArgs.DeliveryTag, false, true);
            
            _logger.LogWarning("Task {TaskId} failed, retrying ({RetryCount}/{MaxRetries}): {Error}", 
                task.Id, task.RetryCount, task.MaxRetries, exception.Message);
        }
        else
        {
            task.Status = Core.Enums.TaskItemStatus.Failed;
            task.CompletedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);

            _channel?.BasicAck(eventArgs.DeliveryTag, false);
            TaskFailed?.Invoke(this, (task, exception));

            _logger.LogError("Task {TaskId} failed permanently after {RetryCount} retries: {Error}", 
                task.Id, task.RetryCount, exception.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopAsync().Wait(TimeSpan.FromSeconds(5));
        
        _channel?.Dispose();
        _connection?.Dispose();
        
        _disposed = true;
    }
}