# RabbitMQ Task Scheduler

A complete task scheduling system built with .NET 8.0, RabbitMQ, Entity Framework Core, and SignalR for real-time monitoring.

## 🚀 Features

### Core Functionality
- **Dynamic Task Scheduling**: Support for immediate, scheduled, delayed, and recurring tasks
- **RabbitMQ Integration**: Reliable message queuing with retry mechanisms
- **Real-time Monitoring**: Live dashboard with SignalR for real-time updates
- **Auto-scaling Consumers**: Dynamic consumer management with horizontal scaling
- **Task Management**: Complete CRUD operations with status tracking
- **Web Dashboard**: Modern responsive web interface for task and consumer management

### Technical Stack
- **.NET 8.0**: Modern C# with latest features
- **RabbitMQ**: Message broker for task queuing
- **Entity Framework Core**: Database ORM with SQLite/SQL Server support
- **SignalR**: Real-time web communication
- **Bootstrap 5**: Responsive UI framework
- **Docker**: Containerized deployment

## 📁 Project Structure

```
RabbitMQTaskScheduler/
├── src/
│   ├── RabbitMQTaskScheduler.Core/           # Domain models and interfaces
│   ├── RabbitMQTaskScheduler.Infrastructure/ # Data access and RabbitMQ services
│   ├── RabbitMQTaskScheduler.Web/           # Web API and UI
│   └── RabbitMQTaskScheduler.Worker/        # Background worker service
├── tests/                                    # Unit and integration tests
├── docker/                                   # Docker configuration files
└── docs/                                    # Documentation
```

## 🛠️ Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker and Docker Compose (optional)
- RabbitMQ server (or use Docker)

### Option 1: Local Development with Docker RabbitMQ

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd TaskScheduler
   ```

2. **Start RabbitMQ using Docker**
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d
   ```

3. **Run the applications**
   ```bash
   # Terminal 1: Start the Web API
   cd src/RabbitMQTaskScheduler.Web
   dotnet run

   # Terminal 2: Start the Worker
   cd src/RabbitMQTaskScheduler.Worker
   dotnet run
   ```

4. **Access the application**
   - Web Dashboard: http://localhost:5000
   - Swagger API: http://localhost:5000/swagger
   - RabbitMQ Management: http://localhost:15672 (guest/guest)

### Option 2: Full Docker Deployment

1. **Deploy using Docker Compose**
   ```bash
   cd docker
   docker-compose up -d
   ```

2. **Access the application**
   - Web Dashboard: http://localhost:8080
   - RabbitMQ Management: http://localhost:15672 (admin/password123)

## 📖 Configuration

### Database Configuration
By default, the system uses SQLite for development. To use SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskScheduler;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### RabbitMQ Configuration
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "QueueName": "task-queue",
    "ExchangeName": "task-exchange",
    "Durable": true,
    "PrefetchCount": 10
  }
}
```

### Task Scheduler Configuration
```json
{
  "TaskScheduler": {
    "DefaultConsumerCount": 3,
    "MaxConsumerCount": 10,
    "MinConsumerCount": 1,
    "RetryPolicy": {
      "MaxRetries": 3,
      "RetryDelaySeconds": 30,
      "BackoffMultiplier": 2,
      "MaxRetryDelaySeconds": 3600
    },
    "ConsumerHeartbeatIntervalSeconds": 30,
    "ConsumerTimeoutSeconds": 300,
    "EnableDelayedTasks": true,
    "StatisticsUpdateIntervalSeconds": 60
  }
}
```

## 🎯 Usage Examples

### Creating Tasks via API

#### Immediate Task
```bash
curl -X POST "http://localhost:5000/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Process Order",
    "description": "Process customer order #12345",
    "type": 0,
    "priority": 1,
    "payload": "{\"orderId\": 12345, \"customerId\": 67890}"
  }'
```

#### Scheduled Task
```bash
curl -X POST "http://localhost:5000/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Send Newsletter",
    "description": "Send weekly newsletter",
    "type": 1,
    "priority": 1,
    "scheduledAt": "2024-12-01T09:00:00Z",
    "payload": "{\"templateId\": \"newsletter-weekly\"}"
  }'
```

#### Delayed Task
```bash
curl -X POST "http://localhost:5000/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Follow-up Email",
    "description": "Send follow-up email after 1 hour",
    "type": 2,
    "priority": 1,
    "delayTime": "01:00:00",
    "payload": "{\"emailType\": \"follow-up\", \"recipientId\": 123}"
  }'
```

### Managing Consumers

#### Scale Up Consumers
```bash
curl -X POST "http://localhost:5000/api/consumers/scale" \
  -H "Content-Type: application/json" \
  -d '{"targetCount": 5}'
```

#### Get Consumer Status
```bash
curl -X GET "http://localhost:5000/api/consumers"
```

## 🖥️ Web Dashboard

The web dashboard provides:

### Dashboard View
- Real-time task statistics
- Performance metrics (success rate, avg execution time)
- Consumer status overview
- Live updates via SignalR

### Task Management
- View all tasks with filtering and search
- Create new tasks with different types
- Monitor task execution status
- Retry failed tasks

### Consumer Management
- View active consumers
- Scale consumers up/down
- Monitor consumer health and performance
- Track task assignments

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/RabbitMQTaskScheduler.Core.Tests/
```

## 🔧 Development

### Adding Custom Task Processors

1. Implement the `ITaskProcessor` interface:
```csharp
public class EmailTaskProcessor : ITaskProcessor
{
    public async Task<TaskExecutionResult> ProcessTaskAsync(TaskItem task, CancellationToken cancellationToken)
    {
        // Custom processing logic
        return new TaskExecutionResult { IsSuccess = true };
    }

    public bool CanHandle(string taskType) => taskType == "email";
}
```

2. Register in DI container:
```csharp
services.AddScoped<ITaskProcessor, EmailTaskProcessor>();
```

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName -p src/RabbitMQTaskScheduler.Infrastructure -s src/RabbitMQTaskScheduler.Web

# Update database
dotnet ef database update -p src/RabbitMQTaskScheduler.Infrastructure -s src/RabbitMQTaskScheduler.Web
```

## 🚀 Deployment

### Production Deployment with Docker

1. **Build and deploy**
   ```bash
   cd docker
   docker-compose -f docker-compose.yml up -d
   ```

2. **Scale workers**
   ```bash
   docker-compose -f docker-compose.yml up -d --scale taskscheduler-worker=5
   ```

### Health Checks

The application includes health checks for:
- Database connectivity
- RabbitMQ connectivity
- Consumer status

Access health checks at: `http://localhost:5000/health`

## 📊 Monitoring

### Built-in Metrics
- Task completion rates
- Average execution times
- Consumer performance
- Queue lengths
- Error rates

### Logging
The application uses structured logging with different levels:
- `Information`: General application flow
- `Warning`: Retry attempts and recoverable errors
- `Error`: Unhandled exceptions and failures
- `Debug`: Detailed execution information (development only)

## 🔐 Security Considerations

### Production Setup
- Change default RabbitMQ credentials
- Use environment variables for sensitive configuration
- Enable HTTPS for web interface
- Implement authentication/authorization for web dashboard
- Use strong database passwords
- Configure firewall rules for exposed ports

### Example Production Environment Variables
```bash
RABBITMQ_USERNAME=your-username
RABBITMQ_PASSWORD=your-secure-password
DB_PASSWORD=your-db-password
ASPNETCORE_ENVIRONMENT=Production
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the GitHub repository
- Check the documentation in the `/docs` folder
- Review the example configurations

## 🗺️ Roadmap

- [ ] Add authentication and authorization
- [ ] Implement task dependencies
- [ ] Add more detailed metrics and monitoring
- [ ] Support for task cancellation
- [ ] Task result caching
- [ ] Advanced scheduling options (cron expressions)
- [ ] Multi-tenant support
- [ ] Performance optimizations
- [ ] Additional database providers