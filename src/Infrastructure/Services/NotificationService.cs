using Core.Entities;
using Core.Interfaces.Notifications;
using FirebaseAdmin.Messaging;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NotificationService(AppDbContext context, ILogger<NotificationService> logger) : INotificationService
{
    public async Task SubscribeToNotificationsAsync(int userId, string fcmToken)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            logger.LogWarning("Tentative d’abonnement avec un token FCM vide pour l’utilisateur {UserId}", userId);
            throw new ArgumentException("Le token FCM est requis.");
        }

        var user = await context.Users.FindAsync(userId)
                   ?? throw new ArgumentException("Utilisateur non trouvé.");

        var existingToken = await context.UserFcmTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == fcmToken);

        if (existingToken == null)
        {
            var newToken = new UserFcmToken
            {
                UserId = userId,
                Token = fcmToken,
                CreatedAt = DateTime.UtcNow
            };
            context.UserFcmTokens.Add(newToken);
            await context.SaveChangesAsync();
            logger.LogInformation("Utilisateur {UserId} abonné aux notifications avec le token {Token}", userId, fcmToken);
        }
    }

    public async Task SendNotificationToUserAsync(int userId, string title, string body)
    {
        var tokens = await context.UserFcmTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        if (!tokens.Any())
        {
            logger.LogWarning("Aucun token FCM trouvé pour l’utilisateur {UserId}", userId);
            return;
        }

        var messages = tokens.Select(token => new Message
        {
            Token = token,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "type", "user_notification" }
            }
        }).ToList();

        await SendMessagesAsync(messages);
    }

    public async Task SendNotificationToTournamentPlayersAsync(int tournamentId, string title, string body)
    {
        var tokens = await context.TournamentPlayers
            .Where(tp => tp.TournamentId == tournamentId)
            .Join(context.UserFcmTokens,
                tp => tp.UserId,
                t => t.UserId,
                (tp, t) => t.Token)
            .Distinct()
            .ToListAsync();

        if (!tokens.Any())
        {
            logger.LogWarning("Aucun token FCM trouvé pour les joueurs du tournoi {TournamentId}", tournamentId);
            return;
        }

        var messages = tokens.Select(token => new Message
        {
            Token = token,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = new Dictionary<string, string>
            {
                { "tournamentId", tournamentId.ToString() },
                { "type", "tournament_notification" }
            }
        }).ToList();

        await SendMessagesAsync(messages);
    }

    private async Task SendMessagesAsync(List<Message> messages)
    {
        try
        {
            const int batchSize = 500; 
            var messageBatches = new List<List<Message>>();
            for (int i = 0; i < messages.Count; i += batchSize)
            {
                messageBatches.Add(messages.Skip(i).Take(batchSize).ToList());
            }

            await Parallel.ForEachAsync(messageBatches, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (batch, ct) =>
            {
                var batchResponse = await FirebaseMessaging.DefaultInstance.SendAllAsync(batch, ct);
                logger.LogInformation("Envoyé {SuccessCount}/{TotalCount} notifications avec succès", batchResponse.SuccessCount, batchResponse.Responses.Count);

                foreach (var response in batchResponse.Responses)
                {
                    if (!response.IsSuccess)
                    {
                        logger.LogWarning("Échec de l’envoi de la notification : {Error}", response.Exception?.Message);
                    }
                }
            });
        }
        catch (FirebaseMessagingException ex)
        {
            logger.LogError(ex, "Erreur lors de l’envoi des notifications via FCM.");
            throw;
        }
    }
}