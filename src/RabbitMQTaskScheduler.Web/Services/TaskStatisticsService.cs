using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Web.Services;

public interface ITaskStatisticsService
{
    Task<TaskStatistics> GetStatisticsAsync();
}

public class TaskStatisticsService : ITaskStatisticsService
{
    private readonly Core.Interfaces.ITaskRepository _taskRepository;

    public TaskStatisticsService(Core.Interfaces.ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskStatistics> GetStatisticsAsync()
    {
        return await _taskRepository.GetStatisticsAsync();
    }
}