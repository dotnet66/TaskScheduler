using Microsoft.Extensions.Options;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Worker;

public class TaskConsumerWorker : BackgroundService
{
    private readonly ILogger<TaskConsumerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TaskSchedulerConfiguration _config;
    private readonly List<ITaskConsumer> _consumers = new();
    private readonly SemaphoreSlim _consumerSemaphore = new(1, 1);

    public TaskConsumerWorker(
        ILogger<TaskConsumerWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<TaskSchedulerConfiguration> config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Consumer Worker starting...");

        try
        {
            // Start initial consumers
            await ScaleConsumersToTarget(_config.DefaultConsumerCount);

            // Start heartbeat and scaling monitoring
            var heartbeatTask = StartHeartbeatLoop(stoppingToken);
            var scalingTask = StartScalingMonitor(stoppingToken);

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Task Consumer Worker stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Task Consumer Worker");
            throw;
        }
        finally
        {
            await StopAllConsumers();
        }
    }

    private async Task StartHeartbeatLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var consumerManager = scope.ServiceProvider.GetRequiredService<IConsumerManager>();

                await _consumerSemaphore.WaitAsync(cancellationToken);
                try
                {
                    foreach (var consumer in _consumers)
                    {
                        if (consumer.IsRunning)
                        {
                            await consumerManager.UpdateConsumerHeartbeatAsync(consumer.ConsumerId);
                        }
                    }
                }
                finally
                {
                    _consumerSemaphore.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.ConsumerHeartbeatIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat loop");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    private async Task StartScalingMonitor(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // This would monitor for scaling requests from the web interface
                // For now, we'll just log the current consumer count
                await _consumerSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var activeCount = _consumers.Count(c => c.IsRunning);
                    _logger.LogDebug("Active consumers: {ActiveCount}", activeCount);
                }
                finally
                {
                    _consumerSemaphore.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scaling monitor");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    private async Task ScaleConsumersToTarget(int targetCount)
    {
        await _consumerSemaphore.WaitAsync();
        try
        {
            var currentCount = _consumers.Count(c => c.IsRunning);
            
            if (targetCount > currentCount)
            {
                // Scale up
                var toAdd = targetCount - currentCount;
                for (int i = 0; i < toAdd; i++)
                {
                    await StartNewConsumer();
                }
            }
            else if (targetCount < currentCount)
            {
                // Scale down
                var toRemove = currentCount - targetCount;
                var runningConsumers = _consumers.Where(c => c.IsRunning).Take(toRemove).ToList();
                
                foreach (var consumer in runningConsumers)
                {
                    await StopConsumer(consumer);
                }
            }

            _logger.LogInformation("Scaled consumers to {TargetCount} (was {CurrentCount})", targetCount, currentCount);
        }
        finally
        {
            _consumerSemaphore.Release();
        }
    }

    private async Task StartNewConsumer()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var consumer = scope.ServiceProvider.GetRequiredService<ITaskConsumer>();
            var consumerManager = scope.ServiceProvider.GetRequiredService<IConsumerManager>();

            // Register consumer
            await consumerManager.RegisterConsumerAsync(consumer.ConsumerId);

            // Set up event handlers
            consumer.TaskProcessed += OnTaskProcessed;
            consumer.TaskFailed += OnTaskFailed;

            // Start consumer
            await consumer.StartAsync();
            _consumers.Add(consumer);

            _logger.LogInformation("Started consumer {ConsumerId}", consumer.ConsumerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new consumer");
        }
    }

    private async Task StopConsumer(ITaskConsumer consumer)
    {
        try
        {
            await consumer.StopAsync();
            
            using var scope = _serviceProvider.CreateScope();
            var consumerManager = scope.ServiceProvider.GetRequiredService<IConsumerManager>();
            await consumerManager.UnregisterConsumerAsync(consumer.ConsumerId);

            // Remove event handlers
            consumer.TaskProcessed -= OnTaskProcessed;
            consumer.TaskFailed -= OnTaskFailed;

            _consumers.Remove(consumer);

            _logger.LogInformation("Stopped consumer {ConsumerId}", consumer.ConsumerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop consumer {ConsumerId}", consumer.ConsumerId);
        }
    }

    private async Task StopAllConsumers()
    {
        await _consumerSemaphore.WaitAsync();
        try
        {
            var stopTasks = _consumers.Select(StopConsumer).ToArray();
            await Task.WhenAll(stopTasks);
            _consumers.Clear();
            
            _logger.LogInformation("Stopped all consumers");
        }
        finally
        {
            _consumerSemaphore.Release();
        }
    }

    private void OnTaskProcessed(object? sender, TaskItem task)
    {
        _logger.LogInformation("Task {TaskId} processed successfully by consumer {ConsumerId}", 
            task.Id, (sender as ITaskConsumer)?.ConsumerId);
    }

    private void OnTaskFailed(object? sender, (TaskItem Task, Exception Exception) args)
    {
        _logger.LogError(args.Exception, "Task {TaskId} failed on consumer {ConsumerId}", 
            args.Task.Id, (sender as ITaskConsumer)?.ConsumerId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task Consumer Worker stopping...");
        await StopAllConsumers();
        await base.StopAsync(cancellationToken);
    }
}
