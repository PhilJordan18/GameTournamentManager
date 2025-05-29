using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Tournament;

public interface ITournamentServices
{
    Task<TournamentResponse> CreateTournamentAsync(TournamentConfig config, int userId);
    Task<List<TournamentResponse>> GetAllTournamentsAsync();
    Task<TournamentResponse> JoinTournamentAsync(JoinTournamentRequest request, int userId);
    Task<StartTournamentResponse> StartTournamentAsync(int tournamentId, int adminId);
    Task<SubmitMatchResultResponse> SubmitMatchResultAsync(int tournamentId, int matchId, SubmitMatchResultRequest request, int userId);
    Task<ValidateMatchResultResponse> ValidateMatchResultAsync(int tournamentId, int matchId, ValidateMatchResultRequest request, int adminId);
    Task ValidateMultipleMatchesAsync(int tournamentId, Dictionary<int, bool> matchValidations, int adminId);
    Task AdvanceTournamentAsync(int tournamentId, int adminId);
}