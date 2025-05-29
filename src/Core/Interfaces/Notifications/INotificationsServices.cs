namespace Core.Interfaces.Notifications;

public interface INotificationService
{
    Task SubscribeToNotificationsAsync(int userId, string fcmToken);
    Task SendNotificationToUserAsync(int userId, string title, string body);
    Task SendNotificationToTournamentPlayersAsync(int tournamentId, string title, string body);
}