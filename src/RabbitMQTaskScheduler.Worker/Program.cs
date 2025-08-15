using RabbitMQTaskScheduler.Infrastructure;
using RabbitMQTaskScheduler.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add the worker service
builder.Services.AddHostedService<TaskConsumerWorker>();

var host = builder.Build();

// Initialize database
await host.Services.InitializeDatabaseAsync();

host.Run();
