using Domain.Interfaces;
using Infrastructure.Data;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task SendNotificationAsync(string message)
    {
        var doc = new Document { Text = message };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ReceiveNotification", message);
    }
}