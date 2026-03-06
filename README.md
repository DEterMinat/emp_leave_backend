# Employee Leave System - Backend API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-47A248?logo=mongodb)](https://www.mongodb.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

ASP.NET Core 8 Web API for Employee Leave Management System with real-time notifications.

## ✨ Features

- 🔐 JWT authentication + role-based authorization (Employee, Manager, HR, Admin)
- 📊 Leave requests, approval/rejection, leave balance tracking
- 🕒 **Attendance Tracking** - Automatic "Late" detection (after 09:00 AM)
- 🔔 SignalR real-time notifications
- 📎 Attachment upload for leave requests
- 📈 Prometheus metrics (`/metrics`)
- 🧪 Unit tests with xUnit + Moq

## 📁 Project Structure

```text
emp_leave_backend/
├── EmployeeLeaveApp.sln
├── docker-compose.yml
├── Dockerfile
├── src/EmployeeLeaveApi/
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   ├── Hubs/
│   ├── Validators/
│   └── Program.cs
└── tests/EmployeeLeaveApi.Tests/
```

## 🚀 Quick Start (Local)

### Prerequisites

- .NET 8 SDK
- MongoDB (local or Atlas)

### Option A: Run from `emp_leave_backend`

```bash
cd emp_leave_backend
dotnet restore EmployeeLeaveApp.sln
dotnet run --project src/EmployeeLeaveApi
```

### Option B: Run from workspace root

```bash
dotnet run --project emp_leave_backend/src/EmployeeLeaveApi
```

### Local URLs (Development)

> Current launch profile uses port **5082** (`launchSettings.json`).

- API Base: http://localhost:5082
- Swagger UI: http://localhost:5082/docs
- Health: http://localhost:5082/health
- Metrics: http://localhost:5082/metrics

## 🐳 Run with Docker

Create `.env` in `emp_leave_backend`:

```bash
echo "MONGODB_URL=mongodb+srv://..." > .env
echo "DB_NAME=emp-leave" >> .env
```

Start services:

```bash
docker-compose up --build
```

Docker access:

- Entry point (Nginx LB): http://localhost:8080
- Backend API via proxy: `http://localhost:8080/api/...`

> Note: Current Nginx config only proxies `/api/*`.
> Paths like `/health` and `/docs` are not exposed through `:8080` by default.

## 🔌 Main API Endpoints

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
| PUT    | `/api/users/{id}` | Update user (Position, Salary, etc.) |

### Attendance

| Method | Endpoint                    | Description                     |
| ------ | --------------------------- | ------------------------------- |
| POST   | `/api/attendance/check-in`  | Check in (Auto-Late detection)  |
| POST   | `/api/attendance/check-out` | Check out                       |
| GET    | `/api/attendance/today`     | Get today's record for current  |
| GET    | `/api/attendance/history`   | Get history for current user    |
| GET    | `/api/attendance/all`       | List all records (Admin/HR only)|

### Database Utilities

| Method | Endpoint                              | Description                          |
| ------ | ------------------------------------- | ------------------------------------ |
| GET    | `/api/DatabaseCheck/status`           | Check collection/data readiness      |
| POST   | `/api/DatabaseCheck/seed-master-data` | Seed role/department/user/masterdata |

## 🧪 Testing

```bash
dotnet test EmployeeLeaveApp.sln
dotnet test --collect:"XPlat Code Coverage"
```

## ⚙️ Configuration

`appsettings.json` (example):

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://...",
    "DatabaseName": "emp-leave"
  },
  "Jwt": {
    "Secret": "your-jwt-secret",
    "Issuer": "EmployeeLeaveApi",
    "ExpirationHours": "24"
  }
}
```

Common environment variables:

| Variable                         | Description                          |
| -------------------------------- | ------------------------------------ |
| `MongoDB__ConnectionString`      | MongoDB connection string            |
| `MongoDB__DatabaseName`          | MongoDB database name                |
| `ConnectionStrings__MongoDB`     | Alternative key used by .NET config  |
| `Jwt__Secret`                    | JWT secret key                       |
| `Jwt__Issuer`                    | JWT issuer/audience                  |
| `ELASTICSEARCH_URL`              | Optional Elasticsearch endpoint      |

## 🛠️ Troubleshooting

### 1) `MSB3021/MSB3027` file locked (`EmployeeLeaveApi.exe is being used by another process`)

Cause: API is already running and you run `dotnet run` again.

Fix:

```powershell
Get-Process EmployeeLeaveApi -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet run --project src/EmployeeLeaveApi
```

### 2) Port already in use (5082)

```powershell
Get-NetTCPConnection -LocalPort 5082 -State Listen
```

If needed, run on another port:

```powershell
dotnet run --project src/EmployeeLeaveApi --urls "http://localhost:5090"
```

### 3) Wrong current directory

If you are not in `emp_leave_backend`, use full/relative path from workspace root:

```bash
dotnet run --project emp_leave_backend/src/EmployeeLeaveApi
```

## 📄 License

MIT License
