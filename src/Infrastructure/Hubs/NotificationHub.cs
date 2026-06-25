using Microsoft.AspNetCore.SignalR;
using Domain.Interfaces;

namespace Infrastructure.Hubs;

public class NotificationHub : Hub, INotificationService
{
    public async Task SendNotificationAsync(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    public async Task Ping(string message)
    {
        await Clients.Caller.SendAsync("Pong", $"Echo: {message}");
    }

    // Join a document collaboration room
    public async Task JoinDocument(int documentId)
    {
        var roomName = $"document-{documentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }

    // Leave a document collaboration room
    public async Task LeaveDocument(int documentId)
    {
        var roomName = $"document-{documentId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }

    // Broadcast a document update to all users in the document room
    public async Task BroadcastDocumentUpdate(int documentId, string text, int version)
    {
        var roomName = $"document-{documentId}";
        await Clients.Group(roomName).SendAsync("ReceiveDocumentUpdate", 
            new { documentId, text, version, updatedBy = Context.ConnectionId });
    }
}