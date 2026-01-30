# Employee Leave System - Backend API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-47A248?logo=mongodb)](https://www.mongodb.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

ASP.NET Core 8 Web API for Employee Leave Management System with real-time notifications.

## ✨ Features

- 🔐 **JWT Authentication** - Secure role-based access (Employee, Manager, HR)
- 📊 **Leave Management** - Request, approve, reject leave with balance tracking
- 🔔 **Real-time Notifications** - SignalR for instant updates
- 📎 **File Uploads** - Attach documents to leave requests
- 🐳 **Docker Ready** - Containerized deployment with Nginx load balancing
- 🧪 **Unit Tested** - xUnit tests with Moq mocking

## 📁 Project Structure

```
emp_leave_backend/
├── EmployeeLeaveApp.sln          # Solution file
├── Dockerfile                    # Multi-stage build
├── docker-compose.yml            # Full stack orchestration
├── src/
│   └── EmployeeLeaveApi/         # Main API project
│       ├── Controllers/          # API endpoints
│       ├── Services/             # Business logic
│       ├── Models/               # MongoDB documents
│       ├── DTOs/                 # Request/Response DTOs
│       ├── Hubs/                 # SignalR hubs
│       ├── Data/                 # MongoDB context
│       └── Program.cs            # Entry point
└── tests/
    └── EmployeeLeaveApi.Tests/   # xUnit tests
```

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- MongoDB (local or Atlas)

### Run Locally

```bash
# Navigate to project
cd emp_leave_backend

# Restore & Run
dotnet restore EmployeeLeaveApp.sln
dotnet run --project src/EmployeeLeaveApi
```

### Run with Docker

```bash
# Create .env file
echo "MONGODB_URL=mongodb+srv://..." > .env
echo "DB_NAME=emp-leave" >> .env

# Start all services
docker-compose up --build
```

## 🔌 API Endpoints

### Authentication

| Method | Endpoint             | Description                  |
| ------ | -------------------- | ---------------------------- |
| POST   | `/api/auth/login`    | Login with username/password |
| POST   | `/api/auth/register` | Register new user            |

### Leave Requests

| Method | Endpoint                             | Description       |
| ------ | ------------------------------------ | ----------------- |
| GET    | `/api/leaverequests`                 | List all requests |
| POST   | `/api/leaverequests`                 | Create request    |
| POST   | `/api/leaverequests/with-attachment` | Create with file  |
| PUT    | `/api/leaverequests/{id}/approve`    | Approve request   |
| PUT    | `/api/leaverequests/{id}/reject`     | Reject request    |

### Users

| Method | Endpoint          | Description    |
| ------ | ----------------- | -------------- |
| GET    | `/api/users`      | List all users |
| GET    | `/api/users/{id}` | Get user by ID |
| PUT    | `/api/users/{id}` | Update user    |

## 📚 Documentation

- **Swagger UI**: http://localhost:8080/docs
- **Health Check**: http://localhost:8080/health

## 🧪 Testing

```bash
# Run all tests
dotnet test EmployeeLeaveApp.sln

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## ⚙️ Configuration

### Environment Variables

| Variable      | Description               | Example             |
| ------------- | ------------------------- | ------------------- |
| `MONGODB_URL` | MongoDB connection string | `mongodb+srv://...` |
| `DB_NAME`     | Database name             | `emp-leave`         |
| `JWT_SECRET`  | JWT signing key           | `your-secret-key`   |

### appsettings.json

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://...",
    "DatabaseName": "emp-leave"
  },
  "JWT": {
    "Secret": "your-jwt-secret",
    "ExpirationMinutes": 60
  }
}
```

## 🐳 Docker Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Nginx     │────▶│   Backend   │────▶│   MongoDB   │
│   :8080     │     │   :8080     │     │   Atlas     │
└─────────────┘     └─────────────┘     └─────────────┘
       │
       ▼
┌─────────────┐
│  Frontend   │
│   :80       │
└─────────────┘
```

## 📄 License

MIT License
