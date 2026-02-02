using FirebaseAdmin.Messaging;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.Data;
using MongoDB.Driver;

namespace EmployeeLeaveApi.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    Task SendNotificationToAllAsync(string title, string body);
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
}
