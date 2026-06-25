namespace Domain.Interfaces;

public interface INotificationService
{
    // This is a contract. We don't care if this is a SignalR Hub, 
    // a database trigger, or a message queue.
    Task SendNotificationAsync(string message);
}