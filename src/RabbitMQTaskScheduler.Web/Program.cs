using RabbitMQTaskScheduler.Infrastructure;
using RabbitMQTaskScheduler.Web.Hubs;
using RabbitMQTaskScheduler.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add web services
builder.Services.AddScoped<ITaskStatisticsService, TaskStatisticsService>();

// Add CORS for SignalR and API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// Serve static files
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<TaskMonitoringHub>("/hubs/taskmonitoring");

// Map default route to serve the web UI
app.MapFallbackToFile("index.html");

app.Run();
