using Domain.Interfaces;

namespace Application.Services;

public class OrderService
{
    private readonly INotificationService _notificationService;

    // Dependency Injection: The Application doesn't know about SignalR
    // It only knows it has a service that can send notifications.
    public OrderService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ProcessOrderAsync(string orderId)
    {
        // ... Logic to save order to database ...
        
        // Trigger the notification
        await _notificationService.SendNotificationAsync($"Order {orderId} has been placed!");
    }
}