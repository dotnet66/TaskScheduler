# Deployment Guide

## Overview
This guide covers various deployment scenarios for the RabbitMQ Task Scheduler system.

## Prerequisites

### System Requirements
- Docker and Docker Compose (recommended)
- .NET 8.0 Runtime (for manual deployment)
- RabbitMQ Server
- SQL Server or SQLite

### Recommended Specifications
- **Development**: 2 CPU cores, 4GB RAM
- **Production**: 4+ CPU cores, 8GB+ RAM
- **Database**: SSD storage recommended

## Docker Deployment (Recommended)

### Production Deployment

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd TaskScheduler
   ```

2. **Configure environment variables**
   Create a `.env` file in the `docker/` directory:
   ```env
   # Database Configuration
   SA_PASSWORD=YourStrongPassword123!
   DB_NAME=TaskScheduler

   # RabbitMQ Configuration
   RABBITMQ_USER=admin
   RABBITMQ_PASSWORD=YourSecurePassword123!
   RABBITMQ_VHOST=taskscheduler

   # Application Configuration
   ASPNETCORE_ENVIRONMENT=Production
   WEB_PORT=8080
   ```

3. **Deploy the stack**
   ```bash
   cd docker
   docker-compose up -d
   ```

4. **Verify deployment**
   ```bash
   # Check service status
   docker-compose ps
   
   # View logs
   docker-compose logs -f taskscheduler-web
   docker-compose logs -f taskscheduler-worker
   ```

5. **Access the application**
   - Web Dashboard: http://your-server:8080
   - RabbitMQ Management: http://your-server:15672

### Development Deployment

For development with local code changes:

1. **Start only external dependencies**
   ```bash
   cd docker
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Run applications locally**
   ```bash
   # Terminal 1 - Web API
   cd src/RabbitMQTaskScheduler.Web
   dotnet run

   # Terminal 2 - Worker
   cd src/RabbitMQTaskScheduler.Worker
   dotnet run
   ```

## Manual Deployment

### Prerequisites
- RabbitMQ Server installed and running
- SQL Server or SQLite configured
- .NET 8.0 Runtime installed

### Steps

1. **Publish applications**
   ```bash
   # Publish Web API
   dotnet publish src/RabbitMQTaskScheduler.Web -c Release -o /opt/taskscheduler/web

   # Publish Worker
   dotnet publish src/RabbitMQTaskScheduler.Worker -c Release -o /opt/taskscheduler/worker
   ```

2. **Configure appsettings.Production.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-sql-server;Database=TaskScheduler;Integrated Security=true;TrustServerCertificate=true;"
     },
     "RabbitMQ": {
       "HostName": "your-rabbitmq-server",
       "UserName": "your-username",
       "Password": "your-password"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

3. **Create systemd services**

   **Web API Service** (`/etc/systemd/system/taskscheduler-web.service`):
   ```ini
   [Unit]
   Description=TaskScheduler Web API
   After=network.target

   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /opt/taskscheduler/web/RabbitMQTaskScheduler.Web.dll
   Restart=always
   RestartSec=10
   User=taskscheduler
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://+:5000

   [Install]
   WantedBy=multi-user.target
   ```

   **Worker Service** (`/etc/systemd/system/taskscheduler-worker.service`):
   ```ini
   [Unit]
   Description=TaskScheduler Worker
   After=network.target

   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /opt/taskscheduler/worker/RabbitMQTaskScheduler.Worker.dll
   Restart=always
   RestartSec=10
   User=taskscheduler

   [Install]
   WantedBy=multi-user.target
   ```

4. **Start services**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable taskscheduler-web
   sudo systemctl enable taskscheduler-worker
   sudo systemctl start taskscheduler-web
   sudo systemctl start taskscheduler-worker
   ```

## Cloud Deployment

### Azure Container Instances

1. **Create resource group**
   ```bash
   az group create --name taskscheduler-rg --location eastus
   ```

2. **Deploy RabbitMQ**
   ```bash
   az container create \
     --resource-group taskscheduler-rg \
     --name rabbitmq \
     --image rabbitmq:3.13-management \
     --ports 5672 15672 \
     --environment-variables RABBITMQ_DEFAULT_USER=admin RABBITMQ_DEFAULT_PASS=password123
   ```

3. **Deploy TaskScheduler**
   ```bash
   # Build and push images to Azure Container Registry
   az acr build --registry your-registry --image taskscheduler-web:latest --file docker/Dockerfile.web .
   az acr build --registry your-registry --image taskscheduler-worker:latest --file docker/Dockerfile.worker .

   # Deploy container instances
   az container create \
     --resource-group taskscheduler-rg \
     --name taskscheduler-web \
     --image your-registry.azurecr.io/taskscheduler-web:latest \
     --ports 8080
   ```

### AWS ECS

1. **Create ECS cluster**
   ```bash
   aws ecs create-cluster --cluster-name taskscheduler-cluster
   ```

2. **Create task definitions**
   ```json
   {
     "family": "taskscheduler-web",
     "networkMode": "awsvpc",
     "requiresCompatibilities": ["FARGATE"],
     "cpu": "256",
     "memory": "512",
     "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
     "containerDefinitions": [
       {
         "name": "web",
         "image": "your-account.dkr.ecr.region.amazonaws.com/taskscheduler-web:latest",
         "portMappings": [
           {
             "containerPort": 8080,
             "protocol": "tcp"
           }
         ]
       }
     ]
   }
   ```

### Google Cloud Run

1. **Build and push image**
   ```bash
   # Build image
   docker build -f docker/Dockerfile.web -t gcr.io/your-project/taskscheduler-web .

   # Push to Google Container Registry
   docker push gcr.io/your-project/taskscheduler-web
   ```

2. **Deploy to Cloud Run**
   ```bash
   gcloud run deploy taskscheduler-web \
     --image gcr.io/your-project/taskscheduler-web \
     --platform managed \
     --port 8080 \
     --allow-unauthenticated
   ```

## Database Setup

### SQL Server

1. **Create database**
   ```sql
   CREATE DATABASE TaskScheduler;
   ```

2. **Run migrations**
   ```bash
   dotnet ef database update -p src/RabbitMQTaskScheduler.Infrastructure -s src/RabbitMQTaskScheduler.Web
   ```

### SQLite (Development)

Database will be created automatically on first run.

## Load Balancing

### Nginx Configuration

```nginx
upstream taskscheduler_web {
    server 127.0.0.1:5000;
    server 127.0.0.1:5001;
}

server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://taskscheduler_web;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://taskscheduler_web;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Monitoring and Health Checks

### Health Check Endpoints
- Web API: `http://your-server:8080/health`
- Application metrics: Available via built-in statistics API

### Log Aggregation

Configure structured logging for production:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "Console": {
      "FormatterName": "json"
    }
  }
}
```

## Security Considerations

### Production Checklist
- [ ] Change default RabbitMQ credentials
- [ ] Use environment variables for secrets
- [ ] Enable HTTPS/TLS
- [ ] Configure firewall rules
- [ ] Use strong database passwords
- [ ] Implement authentication for web dashboard
- [ ] Regular security updates
- [ ] Network segmentation
- [ ] Backup strategies

### Environment Variables for Production
```env
RABBITMQ_USERNAME=secure-username
RABBITMQ_PASSWORD=secure-complex-password
DB_CONNECTION_STRING=secure-connection-string
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTPS_PORT=443
```

## Scaling

### Horizontal Scaling
- Scale worker instances: `docker-compose up -d --scale taskscheduler-worker=5`
- Use load balancer for web instances
- Consider database connection pooling

### Vertical Scaling
- Increase container memory/CPU limits
- Optimize database performance
- Monitor RabbitMQ resource usage

## Backup and Recovery

### Database Backup
```bash
# SQL Server
sqlcmd -S server -Q "BACKUP DATABASE TaskScheduler TO DISK='backup.bak'"

# SQLite
cp taskscheduler.db taskscheduler_backup_$(date +%Y%m%d).db
```

### RabbitMQ Backup
```bash
# Export definitions
rabbitmqctl export_definitions /backup/definitions.json

# Data directory backup
cp -r /var/lib/rabbitmq /backup/rabbitmq_data
```

## Troubleshooting

### Common Issues

1. **Connection refused to RabbitMQ**
   - Verify RabbitMQ is running
   - Check network connectivity
   - Verify credentials and virtual host

2. **Database connection errors**
   - Verify connection string
   - Check database server status
   - Ensure database exists

3. **Tasks not processing**
   - Check worker service logs
   - Verify RabbitMQ queue status
   - Check consumer registration

### Log Analysis
```bash
# View application logs
docker-compose logs -f taskscheduler-web
docker-compose logs -f taskscheduler-worker

# Search for errors
docker-compose logs taskscheduler-web | grep ERROR
```