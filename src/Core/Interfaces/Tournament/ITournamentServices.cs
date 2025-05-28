using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Tournament;

public interface ITournamentServices
{
    Task<TournamentResponse> CreateTournamentAsync(TournamentConfig config, int userId);
    Task<List<TournamentResponse>> GetAllTournamentsAsync();
    Task<TournamentResponse> JoinTournamentAsync(JoinTournamentRequest request, int userId);
    Task<StartTournamentResponse> StartTournamentAsync(int tournamentId, int adminId);
}