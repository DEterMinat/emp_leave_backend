using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Helpers;
using EmployeeLeaveApi.Middleware;
using EmployeeLeaveApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using EmployeeLeaveApi.Hubs;
using Microsoft.AspNetCore.Mvc;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 Starting Employee Leave API...");
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddSignalR();

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddEndpointsApiExplorer();
    
    // Swagger with JWT support
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() 
        { 
            Title = "Employee Leave API", 
            Version = "v1",
            Description = "API for Employee Leave Management System"
        });
        
        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // MongoDB
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();

    // JWT Helper
    builder.Services.AddSingleton<JwtHelper>();

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "DefaultSecretKey123456789012345678901234567890";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmployeeLeaveApi";
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtIssuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // CORS - Allow frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:8080") // Add your frontend origins
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        });

    });

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    QueueLimit = 2,
                    Window = TimeSpan.FromMinutes(1)
                }));
        
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
        };
    });

    var app = builder.Build();

    // Global Exception Handler
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Leave API v1");
            c.RoutePrefix = "docs";
        });
    }

    app.UseSecurityHeaders(); // Security Headers
    app.UseRateLimiter(); // Rate Limiting
    app.UseCors("AllowFrontend");
    app.UseStaticFiles(); // Enable serving static files (uploads)
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<NotificationHub>("/notificationHub");


    // Root endpoint
    app.MapGet("/", () => new { message = "Employee Leave API - Go to /docs for Swagger UI" });

    // Health check with MongoDB test
    app.MapGet("/health", async ([FromServices] IMongoDbContext db) => 
    {
        var mongoConnected = await db.TestConnectionAsync();
        return new { 
            status = mongoConnected ? "healthy" : "unhealthy",
            mongodb = mongoConnected ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow 
        };
    });

    Log.Information("📚 Swagger UI: http://localhost:5000/docs");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
