using Microsoft.AspNetCore.SignalR;

namespace EmployeeLeaveApi.Hubs;

public class NotificationHub : Hub
{
    // Clients can join groups based on their User ID or Role for targeted notifications
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
