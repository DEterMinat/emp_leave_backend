using FirebaseAdmin.Messaging;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.Data;
using MongoDB.Driver;

namespace EmployeeLeaveApi.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    Task SendNotificationToAllAsync(string title, string body);
    Task<List<UserNotification>> GetUserNotificationsAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId);
}

public class NotificationService : INotificationService
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IMongoDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            // 1. Persist notification to Database (New requirement from Design)
            var notification = new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = body,
                Type = data != null && data.ContainsKey("type") ? data["type"] : "Info",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _context.UserNotifications.InsertOneAsync(notification);

            // 2. Send Push Notification via FCM
            // Find all tokens for this user
            var tokens = await _context.DeviceTokens
                .Find(t => t.UserId == userId)
                .ToListAsync();

            if (!tokens.Any())
            {
                _logger.LogWarning($"No device tokens found for user {userId}");
                return;
            }

            foreach (var deviceToken in tokens)
            {
                var message = new Message()
                {
                    Token = deviceToken.Token,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data
                };

                try
                {
                    // Check if Firebase is configured before sending
                    if (FirebaseMessaging.DefaultInstance == null)
                    {
                        _logger.LogWarning("Firebase not initialized. Skipping FCM send.");
                        return;
                    }

                    await FirebaseMessaging.DefaultInstance.SendAsync(message);
                }
                catch (FirebaseMessagingException ex)
                {
                    if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || 
                        ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                    {
                        // Token invalid/expired, remove it
                        await _context.DeviceTokens.DeleteOneAsync(t => t.Id == deviceToken.Id);
                    }
                    _logger.LogError(ex, $"Error sending FCM to token {deviceToken.Token}");
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error in SendNotificationAsync");
        }
    }

    public async Task SendNotificationToAllAsync(string title, string body)
    {
        // Implementation for topics or batch sending
    }

    public async Task<List<UserNotification>> GetUserNotificationsAsync(string userId)
    {
        return await _context.UserNotifications
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Limit(50)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        var update = Builders<UserNotification>.Update.Set(n => n.IsRead, true);
        var result = await _context.UserNotifications.UpdateOneAsync(n => n.Id == notificationId, update);
        return result.ModifiedCount > 0;
    }
}
