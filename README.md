# Employee Leave System Backend

A FastAPI-based backend for managing employee leave requests with MongoDB integration.

## Features

- User management (Create, Read, Update, Delete)
- MongoDB database integration
- Automatic API documentation with Swagger UI
- Health check endpoint
- Input validation with Pydantic

## Prerequisites

- Python 3.8+
- MongoDB Atlas account (or local MongoDB instance)

## Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd emp_leave_backend
   ```

2. **Create virtual environment (optional but recommended):**
   ```bash
   python -m venv venv
   venv\Scripts\activate  # On Windows
   ```

3. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

4. **Set up environment variables:**
   ```bash
   cp .env.example .env
   ```
   Edit `.env` with your MongoDB Atlas connection string (get it from your Atlas dashboard).

5. **Run the application:**
   ```bash
   # Using the batch file
   run.bat

   # Or directly with uvicorn
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
   ```

## API Endpoints

- `GET /` - Welcome message
- `GET /health` - Database health check
- `POST /users/` - Create new user
- `GET /users/` - List all users
- `GET /users/{id}` - Get user by ID
- `PUT /users/{id}` - Update user
- `DELETE /users/{id}` - Delete user

## API Documentation

When the server is running, visit:
- **Swagger UI:** http://localhost:8000/docs
- **ReDoc:** http://localhost:8000/redoc

## Project Structure

```
app/
├── core/
│   ├── config.py      # Settings and configuration
│   └── database.py    # MongoDB connection
├── models/            # Database models (if needed)
├── routers/
│   ├── example.py     # Basic routes
│   └── users.py       # User management routes
├── schemas/
│   └── user.py        # Pydantic schemas for users
└── main.py            # FastAPI application
```

## Development

- Uses FastAPI for the web framework
- Motor for async MongoDB operations
- Pydantic for data validation
- Uvicorn as ASGI server

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

[Add license information here]