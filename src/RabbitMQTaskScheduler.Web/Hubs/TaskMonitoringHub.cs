using Microsoft.AspNetCore.SignalR;
using RabbitMQTaskScheduler.Core.Models;

namespace RabbitMQTaskScheduler.Web.Hubs;

public class TaskMonitoringHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendTaskUpdate(TaskItem task)
    {
        await Clients.All.SendAsync("TaskUpdated", task);
    }

    public async Task SendStatisticsUpdate(TaskStatistics statistics)
    {
        await Clients.All.SendAsync("StatisticsUpdated", statistics);
    }

    public async Task SendConsumerUpdate(IEnumerable<ConsumerInfo> consumers)
    {
        await Clients.All.SendAsync("ConsumersUpdated", consumers);
    }
}