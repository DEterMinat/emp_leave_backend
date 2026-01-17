# Employee Leave System - Backend

ASP.NET Core 8 Web API สำหรับระบบจัดการลางานพนักงาน

## 🛠️ Tech Stack

- **.NET 8** - Web API Framework
- **MongoDB** - Database
- **Swagger/OpenAPI** - API Documentation

## 📁 โครงสร้าง

```
EmployeeLeaveApi/
├── Controllers/
│   └── UsersController.cs      # User CRUD endpoints
├── Models/
│   └── User.cs                 # MongoDB document model
├── DTOs/
│   └── UserDtos.cs             # Request/Response DTOs
├── Services/
│   ├── IUserService.cs         # Service interface
│   └── UserService.cs          # Business logic
├── Data/
│   └── MongoDbContext.cs       # MongoDB connection
├── Program.cs                  # Entry point + DI
└── appsettings.json            # Configuration
```

## 🚀 การรัน

```bash
# Restore packages
dotnet restore

# Run development
dotnet run

# หรือ watch mode
dotnet watch run
```

## 📚 API Documentation

- **Swagger UI**: http://localhost:5000/docs
- **Health Check**: http://localhost:5000/health

## 🔌 API Endpoints

| Method | Endpoint          | Description     |
| ------ | ----------------- | --------------- |
| GET    | `/api/users`      | List all users  |
| GET    | `/api/users/{id}` | Get user by ID  |
| POST   | `/api/users`      | Create new user |
| PUT    | `/api/users/{id}` | Update user     |
| DELETE | `/api/users/{id}` | Delete user     |

## ⚙️ Configuration

แก้ไข MongoDB connection ใน `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://...",
    "DatabaseName": "emp-leave"
  }
}
```
