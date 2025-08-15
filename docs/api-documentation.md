# API Documentation

## Overview
The RabbitMQ Task Scheduler provides a RESTful API for managing tasks and consumers. All endpoints return JSON responses and use standard HTTP status codes.

## Base URL
- Development: `http://localhost:5000/api`
- Production: `http://your-domain/api`

## Authentication
Currently, the API does not require authentication. In production, implement proper authentication mechanisms.

## Tasks API

### Get All Tasks
```http
GET /api/tasks?page=1&pageSize=20&search=&status=
```

**Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)
- `search` (optional): Search term for task name/description
- `status` (optional): Filter by task status (0-5)

**Response:**
```json
[
  {
    "id": "string",
    "name": "string",
    "description": "string",
    "type": 0,
    "status": 0,
    "priority": 0,
    "payload": "string",
    "createdAt": "2024-01-01T00:00:00Z",
    "scheduledAt": "2024-01-01T00:00:00Z",
    "startedAt": "2024-01-01T00:00:00Z",
    "completedAt": "2024-01-01T00:00:00Z",
    "result": "string",
    "errorMessage": "string",
    "retryCount": 0,
    "maxRetries": 3,
    "executionDuration": "00:00:01",
    "cronExpression": "string",
    "delayTime": "00:01:00",
    "assignedConsumer": "string",
    "metadata": {}
  }
]
```

### Get Task by ID
```http
GET /api/tasks/{id}
```

**Response:** Single task object or 404 if not found.

### Create Task
```http
POST /api/tasks
Content-Type: application/json

{
  "name": "string",
  "description": "string",
  "type": 0,
  "priority": 1,
  "payload": "{}",
  "scheduledAt": "2024-01-01T00:00:00Z",
  "delayTime": "00:01:00",
  "cronExpression": "0 0 * * *",
  "maxRetries": 3,
  "metadata": {}
}
```

**Task Types:**
- `0`: Immediate
- `1`: Scheduled (requires `scheduledAt`)
- `2`: Delayed (requires `delayTime`)
- `3`: Recurring (requires `cronExpression`)

**Priority Levels:**
- `0`: Low
- `1`: Normal
- `2`: High
- `3`: Critical

**Response:** Created task object with 201 status.

### Create Batch Tasks
```http
POST /api/tasks/batch
Content-Type: application/json

[
  {
    "name": "Task 1",
    "type": 0,
    "payload": "{}"
  },
  {
    "name": "Task 2",
    "type": 0,
    "payload": "{}"
  }
]
```

**Response:** Array of created task objects.

### Update Task
```http
PUT /api/tasks/{id}
Content-Type: application/json

{
  "name": "Updated name",
  "description": "Updated description",
  "priority": 2,
  "metadata": {}
}
```

**Response:** Updated task object.

### Delete Task
```http
DELETE /api/tasks/{id}
```

**Response:** 204 No Content on success.

### Get Task Statistics
```http
GET /api/tasks/statistics
```

**Response:**
```json
{
  "totalTasks": 100,
  "pendingTasks": 10,
  "processingTasks": 5,
  "completedTasks": 80,
  "failedTasks": 5,
  "activeConsumers": 3,
  "averageExecutionTime": 1500.0,
  "successRate": 94.1,
  "lastUpdated": "2024-01-01T00:00:00Z"
}
```

## Consumers API

### Get All Consumers
```http
GET /api/consumers
```

**Response:**
```json
[
  {
    "id": "string",
    "name": "string",
    "isActive": true,
    "lastHeartbeat": "2024-01-01T00:00:00Z",
    "processedTasksCount": 50,
    "failedTasksCount": 2,
    "startedAt": "2024-01-01T00:00:00Z",
    "currentTaskId": "string",
    "machineName": "string",
    "processId": 1234
  }
]
```

### Get Consumer Count
```http
GET /api/consumers/count
```

**Response:**
```json
3
```

### Scale Consumers
```http
POST /api/consumers/scale
Content-Type: application/json

{
  "targetCount": 5
}
```

**Response:**
```json
{
  "message": "Scaling request sent for 5 consumers"
}
```

### Unregister Consumer
```http
DELETE /api/consumers/{consumerId}
```

**Response:** 204 No Content on success.

## Status Codes

- `200 OK`: Request successful
- `201 Created`: Resource created successfully
- `204 No Content`: Request successful, no content returned
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

## Error Response Format
```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "string",
  "instance": "string"
}
```

## Rate Limiting
Currently no rate limiting is implemented. Consider implementing rate limiting for production use.

## Swagger Documentation
Interactive API documentation is available at `/swagger` when running in development mode.