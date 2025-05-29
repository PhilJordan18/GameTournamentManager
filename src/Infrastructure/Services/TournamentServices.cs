using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Tournament;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class TournamentServices(AppDbContext context, ILogger<TournamentServices> logger) : ITournamentServices
{
    public async Task<TournamentResponse> CreateTournamentAsync(TournamentConfig config, int userId)
    {
        if (!IsPowerOfTwo(config.NbMaxPlayers))
        {
            logger.LogWarning("Tentative de création d'un tournoi avec un nombre de joueurs invalides");
        }

        var game = await context.Games.FirstOrDefaultAsync(g => g.Name == config.GameName) ?? throw new ArgumentException($"Jeu '{config.GameName}' non trouvé.");
        var tournament = new Tournament
        {
            Name = config.Name,
            GameId = game.Id,
            CreatorId = userId,
            NbMaxPlayers = config.NbMaxPlayers,
            BeginningDate = config.BeginningDate,
            EndDate = config.EndDate,
            StatusId = 1,
            Players = new List<TournamentPlayer>(),
            Matches = new List<Match>(),
            Description = config.Description
        };

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync();
        logger.LogInformation("Tournoi {TournamentId} créé par l’utilisateur {UserId}", tournament.Id, userId);
        return new TournamentResponse
        {
            Id = tournament.Id,
            Name = tournament.Name,
            GameName = game.Name,
            Status = "En attente",
            NbMaxPlayers = tournament.NbMaxPlayers,
            CurrentPlayers = 0,
            BeginningDate = tournament.BeginningDate,
            EndDate = tournament.EndDate,
            Description = tournament.Description
        }; 
    }

    public async Task<List<TournamentResponse>> GetAllTournamentsAsync()
    {
        var tournaments = await context.Tournaments.Include(t => t.Game).Include(t => t.Players)
            .Include(t => t.Status).Select(t => new TournamentResponse
            {
                Id = t.Id,
                Name = t.Name,
                GameName = t.Game.Name,
                Status = t.Status.Name,
                NbMaxPlayers = t.NbMaxPlayers,
                CurrentPlayers = t.Players.Count,
                BeginningDate = t.BeginningDate,
                EndDate = t.EndDate,
                Description = t.Description
            }).ToListAsync();
        
        logger.LogInformation("Récupération de {Count} tournois", tournaments.Count);
        return tournaments;
    }

    public async Task<TournamentResponse> JoinTournamentAsync(JoinTournamentRequest request, int userId)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Game)
            .Include(t => t.Status)
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId)
            ?? throw new ArgumentException("Tournoi non trouvé.");

        if (tournament.StatusId != 1)
        {
            logger.LogWarning("Tentative de rejoindre le tournoi {TournamentId} qui n’est pas en attente", request.TournamentId);
            throw new InvalidOperationException("Le tournoi n’est pas ouvert aux inscriptions.");
        }
        if (tournament.Players.Count >= tournament.NbMaxPlayers)
        {
            logger.LogWarning("Tentative de rejoindre le tournoi {TournamentId} qui est complet", request.TournamentId);
            throw new InvalidOperationException("Le tournoi est complet.");
        }
        if (tournament.Players.Any(tp => tp.UserId == userId))
        {
            logger.LogWarning("L’utilisateur {UserId} a tenté de rejoindre le tournoi {TournamentId} auquel il est déjà inscrit", userId, request.TournamentId);
            throw new InvalidOperationException("Vous êtes déjà inscrit à ce tournoi.");
        }

        var user = await context.Users.FindAsync(userId)
            ?? throw new ArgumentException("Utilisateur non trouvé.");

        var tournamentPlayer = new TournamentPlayer
        {
            UserId = userId,
            TournamentId = request.TournamentId,
            JoinedAt = DateTime.UtcNow
        };

        context.TournamentPlayers.Add(tournamentPlayer);
        await context.SaveChangesAsync();

        logger.LogInformation("L’utilisateur {UserId} a rejoint le tournoi {TournamentId}", userId, request.TournamentId);

        return new TournamentResponse
        {
            Id = tournament.Id,
            Name = tournament.Name,
            GameName = tournament.Game.Name,
            Status = tournament.Status.Name,
            NbMaxPlayers = tournament.NbMaxPlayers,
            CurrentPlayers = tournament.Players.Count,
            BeginningDate = tournament.BeginningDate,
            EndDate = tournament.EndDate,
            Description = tournament.Description
        };
    }

    public async Task<StartTournamentResponse> StartTournamentAsync(int tournamentId, int adminId)
    {
        var admin = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId)
            ?? throw new UnauthorizedAccessException("Utilisateur non trouvé.");

        if (admin.Role.Name != "Admin")
        {
            logger.LogWarning("L’utilisateur {UserId} a tenté de démarrer le tournoi {TournamentId} sans être Admin", adminId, tournamentId);
            throw new UnauthorizedAccessException("Seuls les administrateurs peuvent démarrer un tournoi.");
        }

        var tournament = await context.Tournaments
            .Include(t => t.Players)
            .ThenInclude(tp => tp.User)
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new ArgumentException("Tournoi non trouvé.");

        if (tournament.StatusId != 1) 
        {
            logger.LogWarning("Tentative de démarrer le tournoi {TournamentId} qui n’est pas en attente", tournamentId);
            throw new InvalidOperationException("Le tournoi ne peut pas être démarré.");
        }
        if (tournament.Players.Count < tournament.NbMaxPlayers / 2)
        {
            logger.LogWarning("Tentative de démarrer le tournoi {TournamentId} avec trop peu de joueurs : {CurrentPlayers}/{NbMaxPlayers}", tournamentId, tournament.Players.Count, tournament.NbMaxPlayers);
            throw new InvalidOperationException("Pas assez de joueurs pour démarrer le tournoi.");
        }

        tournament.StatusId = 2; 
        await context.SaveChangesAsync();

        var players = tournament.Players
            .Select(tp => tp.UserId)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var matches = new List<Match>();
        for (var i = 0; i < players.Count; i += 2)
        {
            var match = new Match
            {
                Name = $"Match {(i / 2) + 1}",
                TournamentId = tournamentId,
                FirstPlayerId = players[i],
                SecondPlayerId = i + 1 < players.Count ? players[i + 1] : null,
                MatchTime = tournament.BeginningDate,
                PendingWinnerId = null,
                WinnerId = null
            };
            matches.Add(match);
        }

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();

        logger.LogInformation("Tournoi {TournamentId} démarré par l’Admin {AdminId} avec {MatchCount} matchs initiaux", tournamentId, adminId, matches.Count);

        var matchResponses = await context.Matches
            .Where(m => m.TournamentId == tournamentId)
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .Include(m => m.PendingWinner)
            .Include(m => m.Winner)
            .Select(m => new MatchResponse
            {
                Id = m.Id,
                Name = m.Name,
                TournamentId = m.TournamentId,
                FirstPlayerId = m.FirstPlayerId,
                FirstPlayerUsername = m.FirstPlayer.Username,
                SecondPlayerId = m.SecondPlayerId,
                SecondPlayerUsername = m.SecondPlayer != null ? m.SecondPlayer.Username : null,
                PendingWinnerId = m.PendingWinnerId,
                PendingWinnerUsername = m.PendingWinner != null ? m.PendingWinner.Username : null,
                WinnerId = m.WinnerId,
                WinnerUsername = m.Winner != null ? m.Winner.Username : null,
                MatchTime = m.MatchTime
            })
            .ToListAsync();

        return new StartTournamentResponse
        {
            TournamentId = tournamentId,
            Message = "Tournoi démarré avec succès.",
            InitialMatches = matchResponses
        };
    }

    public async Task<SubmitMatchResultResponse> SubmitMatchResultAsync(int tournamentId, int matchId, SubmitMatchResultRequest request, int userId)
    {
        var match = await context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.FirstPlayer)
            .Include(m => m.SecondPlayer)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId)
            ?? throw new ArgumentException("Match ou tournoi non trouvé.");

        if (match.Tournament.StatusId != 2)
        {
            logger.LogWarning("Tentative de soumettre un résultat pour le match {MatchId} dans un tournoi {TournamentId} qui n’est pas en cours", matchId, tournamentId);
            throw new InvalidOperationException("Le tournoi n’est pas en cours.");
        }

        if (match.FirstPlayerId != userId && match.SecondPlayerId != userId)
        {
            logger.LogWarning("L’utilisateur {UserId} a tenté de soumettre un résultat pour le match {MatchId} auquel il ne participe pas", userId, matchId);
            throw new UnauthorizedAccessException("Seuls les participants du match peuvent soumettre un résultat.");
        }

        if (match.PendingWinnerId != null)
        {
            logger.LogWarning("Tentative de soumettre un résultat pour le match {MatchId} qui a déjà un résultat en attente", matchId);
            throw new InvalidOperationException("Un résultat a déjà été soumis pour ce match.");
        }

        if (request.WinnerId != match.FirstPlayerId && request.WinnerId != match.SecondPlayerId)
        {
            logger.LogWarning("L’utilisateur {UserId} a soumis un WinnerId {WinnerId} invalide pour le match {MatchId}", userId, request.WinnerId, matchId);
            throw new ArgumentException("Le winner doit être l’un des participants du match.");
        }

        match.PendingWinnerId = request.WinnerId;
        await context.SaveChangesAsync();

        logger.LogInformation("Résultat soumis pour le match {MatchId} par l’utilisateur {UserId}", matchId, userId);

        return new SubmitMatchResultResponse
        {
            MatchId = matchId,
            Message = "Résultat soumis avec succès, en attente de validation."
        };
    }

    public async Task<ValidateMatchResultResponse> ValidateMatchResultAsync(int tournamentId, int matchId, ValidateMatchResultRequest request, int adminId)
    {
        var admin = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId)
            ?? throw new UnauthorizedAccessException("Utilisateur non trouvé.");

        if (admin.Role.Name != "Admin")
        {
            logger.LogWarning("L’utilisateur {UserId} a tenté de valider le match {MatchId} sans être Admin", adminId, matchId);
            throw new UnauthorizedAccessException("Seuls les administrateurs peuvent valider un résultat.");
        }

        var match = await context.Matches
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId)
            ?? throw new ArgumentException("Match ou tournoi non trouvé.");

        if (match.PendingWinnerId == null)
        {
            logger.LogWarning("Tentative de valider le match {MatchId} sans résultat soumis", matchId);
            throw new InvalidOperationException("Aucun résultat n’a été soumis pour ce match.");
        }

        if (request.IsApproved)
        {
            match.WinnerId = match.PendingWinnerId;
            match.PendingWinnerId = null;
            logger.LogInformation("Résultat du match {MatchId} validé par l’Admin {AdminId}", matchId, adminId);
        }
        else
        {
            match.PendingWinnerId = null;
            logger.LogInformation("Résultat du match {MatchId} rejeté par l’Admin {AdminId}", matchId, adminId);
        }

        await context.SaveChangesAsync();


        await CheckAndAdvanceTournamentAsync(tournamentId);

        return new ValidateMatchResultResponse
        {
            MatchId = matchId,
            Message = request.IsApproved ? "Résultat validé avec succès." : "Résultat rejeté."
        };
    }
    
    public async Task ValidateMultipleMatchesAsync(int tournamentId, Dictionary<int, bool> matchValidations, int adminId)
    {
        var admin = await context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == adminId)
                    ?? throw new UnauthorizedAccessException("Utilisateur non trouvé.");

        if (admin.Role.Name != "Admin")
            throw new UnauthorizedAccessException("Seuls les administrateurs peuvent valider des résultats.");

        var matches = await context.Matches
            .Where(m => m.TournamentId == tournamentId && matchValidations.Keys.Contains(m.Id))
            .ToListAsync();

        Parallel.ForEach(matches, new ParallelOptions { MaxDegreeOfParallelism = 10 }, match =>
        {
            if (match.PendingWinnerId == null) return;

            if (matchValidations.TryGetValue(match.Id, out var isApproved) && isApproved)
            {
                match.WinnerId = match.PendingWinnerId;
            }
            match.PendingWinnerId = null;
        });

        await context.SaveChangesAsync();
        await CheckAndAdvanceTournamentAsync(tournamentId);
    }

    public async Task AdvanceTournamentAsync(int tournamentId, int adminId)
    {
        var admin = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId)
            ?? throw new UnauthorizedAccessException("Utilisateur non trouvé.");

        if (admin.Role.Name != "Admin")
        {
            logger.LogWarning("L’utilisateur {UserId} a tenté d’avancer le tournoi {TournamentId} sans être Admin", adminId, tournamentId);
            throw new UnauthorizedAccessException("Seuls les administrateurs peuvent avancer le tournoi.");
        }

        await CheckAndAdvanceTournamentAsync(tournamentId);
    }

    private async Task CheckAndAdvanceTournamentAsync(int tournamentId)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Matches)
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new ArgumentException("Tournoi non trouvé.");

        if (tournament.StatusId != 2)
            return;

        var matches = tournament.Matches.ToList();
        if (matches.Any(m => m.WinnerId == null))
            return;

        if (matches.Count == 1)
        {
            tournament.StatusId = 3; 
            await context.SaveChangesAsync();
            logger.LogInformation("Tournoi {TournamentId} terminé", tournamentId);
            return;
        }

        var winners = matches
            .Where(m => m.WinnerId != null)
            .Select(m => m.WinnerId!.Value)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var newMatches = new List<Match>();
        for (int i = 0; i < winners.Count; i += 2)
        {
            var match = new Match
            {
                Name = $"Match {(i / 2) + 1} - Tour suivant",
                TournamentId = tournamentId,
                FirstPlayerId = winners[i],
                SecondPlayerId = i + 1 < winners.Count ? winners[i + 1] : null,
                MatchTime = DateTime.UtcNow.AddHours(1)
            };
            newMatches.Add(match);
        }

        await context.Matches.AddRangeAsync(newMatches);
        await context.SaveChangesAsync();

        logger.LogInformation("Tournoi {TournamentId} avancé au tour suivant avec {MatchCount} nouveaux matchs", tournamentId, newMatches.Count);
    }
    
    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}