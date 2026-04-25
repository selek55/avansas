using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Avansas.Web.Hubs;

public class ChatHub : Hub
{
    // Customer sends a message
    public async Task SendMessage(string message)
    {
        var user = Context.User?.Identity?.Name ?? "Misafir";
        var connectionId = Context.ConnectionId;

        // Add user to their own group
        await Groups.AddToGroupAsync(connectionId, $"user_{connectionId}");

        // Broadcast to admin group with sender info
        await Clients.Group("Admins").SendAsync("ReceiveMessage", new
        {
            connectionId,
            user,
            message,
            timestamp = DateTime.UtcNow.ToString("HH:mm"),
            isAdmin = false
        });

        // Echo back to sender
        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            connectionId,
            user,
            message,
            timestamp = DateTime.UtcNow.ToString("HH:mm"),
            isAdmin = false
        });
    }

    // Admin sends reply to a specific user
    [Authorize(Roles = "Admin,Manager")]
    public async Task SendReply(string targetConnectionId, string message)
    {
        var adminName = Context.User?.Identity?.Name ?? "Admin";

        var payload = new
        {
            connectionId = Context.ConnectionId,
            user = adminName,
            message,
            timestamp = DateTime.UtcNow.ToString("HH:mm"),
            isAdmin = true
        };

        // Send to the target user
        await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", payload);

        // Send to all admins so they see the reply
        await Clients.Group("Admins").SendAsync("ReceiveMessage", new
        {
            connectionId = targetConnectionId,
            user = adminName,
            message,
            timestamp = DateTime.UtcNow.ToString("HH:mm"),
            isAdmin = true
        });
    }

    // Admin joins the admin group
    [Authorize(Roles = "Admin,Manager")]
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    // Notify admins when a user connects
    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "Misafir";
        await Clients.Group("Admins").SendAsync("UserConnected", new
        {
            connectionId = Context.ConnectionId,
            user
        });
        await base.OnConnectedAsync();
    }

    // Notify admins when a user disconnects
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.Group("Admins").SendAsync("UserDisconnected", new
        {
            connectionId = Context.ConnectionId
        });
        await base.OnDisconnectedAsync(exception);
    }
}
