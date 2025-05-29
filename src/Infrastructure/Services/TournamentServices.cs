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
                SecondPlayerId = i + 1 < players.Count ? players[i + 1] : 0,
                MatchTime = tournament.BeginningDate,
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
            .Include(m => m.Winner)
            .Select(m => new MatchResponse
            {
                Id = m.Id,
                Name = m.Name,
                TournamentId = m.TournamentId,
                FirstPlayerId = m.FirstPlayerId,
                FirstPlayerUsername = m.FirstPlayer != null ? m.FirstPlayer.Username : null,
                SecondPlayerId = m.SecondPlayerId,
                SecondPlayerUsername = m.SecondPlayer != null ? m.SecondPlayer.Username : null,
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
    
    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}