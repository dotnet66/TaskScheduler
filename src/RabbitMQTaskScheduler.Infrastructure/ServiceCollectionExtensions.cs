using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using RabbitMQTaskScheduler.Infrastructure.Data;
using RabbitMQTaskScheduler.Infrastructure.RabbitMQ;
using RabbitMQTaskScheduler.Infrastructure.Repositories;
using RabbitMQTaskScheduler.Infrastructure.Services;

namespace RabbitMQTaskScheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<RabbitMQConfiguration>(configuration.GetSection("RabbitMQ"));
        services.Configure<TaskSchedulerConfiguration>(configuration.GetSection("TaskScheduler"));

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use SQLite as default
            services.AddDbContext<TaskSchedulerDbContext>(options =>
                options.UseSqlite("Data Source=taskscheduler.db"));
        }
        else if (connectionString.Contains("Server=") || connectionString.Contains("Data Source=") && connectionString.Contains("Initial Catalog="))
        {
            // SQL Server
            services.AddDbContext<TaskSchedulerDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            // SQLite
            services.AddDbContext<TaskSchedulerDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        // Repositories
        services.AddScoped<ITaskRepository, TaskRepository>();

        // Services
        services.AddScoped<IConsumerManager, ConsumerManager>();
        services.AddScoped<ITaskProcessor, DefaultTaskProcessor>();
        services.AddScoped<ITaskProducer, RabbitMQTaskProducer>();
        services.AddTransient<ITaskConsumer, RabbitMQTaskConsumer>();

        return services;
    }

    public static async Task<IServiceProvider> InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskSchedulerDbContext>();
        
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't fail startup
            Console.WriteLine($"Database initialization warning: {ex.Message}");
        }

        return serviceProvider;
    }
}