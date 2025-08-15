using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using RabbitMQTaskScheduler.Infrastructure.Data;

namespace RabbitMQTaskScheduler.Infrastructure.Services;

public class ConsumerManager : IConsumerManager
{
    private readonly TaskSchedulerDbContext _context;
    private readonly ILogger<ConsumerManager> _logger;
    private readonly TaskSchedulerConfiguration _config;
    private readonly Dictionary<string, ConsumerInfo> _consumers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ConsumerManager(
        TaskSchedulerDbContext context,
        ILogger<ConsumerManager> logger,
        IOptions<TaskSchedulerConfiguration> config)
    {
        _context = context;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<IEnumerable<ConsumerInfo>> GetActiveConsumersAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Remove stale consumers (no heartbeat for more than timeout)
            var timeout = TimeSpan.FromSeconds(_config.ConsumerTimeoutSeconds);
            var cutoff = DateTime.UtcNow - timeout;

            var staleConsumers = await _context.Consumers
                .Where(c => c.LastHeartbeat < cutoff)
                .ToListAsync();

            if (staleConsumers.Any())
            {
                _context.Consumers.RemoveRange(staleConsumers);
                await _context.SaveChangesAsync();

                foreach (var consumer in staleConsumers)
                {
                    _consumers.Remove(consumer.Id);
                    _logger.LogWarning("Removed stale consumer {ConsumerId}", consumer.Id);
                }
            }

            return await _context.Consumers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ConsumerInfo> RegisterConsumerAsync(string consumerId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var consumer = await _context.Consumers.FindAsync(consumerId);
            if (consumer == null)
            {
                consumer = new ConsumerInfo
                {
                    Id = consumerId,
                    Name = $"Consumer-{consumerId[..8]}",
                    IsActive = true,
                    LastHeartbeat = DateTime.UtcNow,
                    StartedAt = DateTime.UtcNow
                };

                _context.Consumers.Add(consumer);
            }
            else
            {
                consumer.IsActive = true;
                consumer.LastHeartbeat = DateTime.UtcNow;
                consumer.StartedAt = DateTime.UtcNow;
                consumer.ProcessedTasksCount = 0;
                consumer.FailedTasksCount = 0;
                consumer.CurrentTaskId = null;
            }

            await _context.SaveChangesAsync();
            _consumers[consumerId] = consumer;

            _logger.LogInformation("Registered consumer {ConsumerId}", consumerId);
            return consumer;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UnregisterConsumerAsync(string consumerId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var consumer = await _context.Consumers.FindAsync(consumerId);
            if (consumer != null)
            {
                consumer.IsActive = false;
                consumer.CurrentTaskId = null;
                await _context.SaveChangesAsync();
            }

            _consumers.Remove(consumerId);
            _logger.LogInformation("Unregistered consumer {ConsumerId}", consumerId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateConsumerHeartbeatAsync(string consumerId)
    {
        var consumer = await _context.Consumers.FindAsync(consumerId);
        if (consumer != null)
        {
            consumer.LastHeartbeat = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (_consumers.ContainsKey(consumerId))
            {
                _consumers[consumerId] = consumer;
            }
        }
    }

    public async Task<int> GetActiveConsumerCountAsync()
    {
        return await _context.Consumers.CountAsync(c => c.IsActive);
    }

    public async Task ScaleConsumersAsync(int targetCount)
    {
        // This is a placeholder for consumer scaling logic
        // In a real implementation, this would coordinate with a consumer factory
        // to start/stop consumer instances
        
        var currentCount = await GetActiveConsumerCountAsync();
        
        if (targetCount < _config.MinConsumerCount)
            targetCount = _config.MinConsumerCount;
        
        if (targetCount > _config.MaxConsumerCount)
            targetCount = _config.MaxConsumerCount;

        if (targetCount == currentCount)
        {
            _logger.LogInformation("Consumer count is already at target: {Count}", targetCount);
            return;
        }

        _logger.LogInformation("Scaling consumers from {CurrentCount} to {TargetCount}", currentCount, targetCount);
        
        // Note: Actual scaling implementation would be handled by the hosting service
        // This method mainly serves to validate and log the scaling request
    }
}