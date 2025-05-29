using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class TournamentConfig
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string GameName { get; set; }
    [Required, Range(4, 32)]
    public int NbMaxPlayers { get; set; } 
    [Required]
    public DateTime BeginningDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty; 
}

public class TournamentResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int NbMaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public DateTime BeginningDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class JoinTournamentRequest
{
    public int TournamentId { get; set; }
}

public class StartTournamentResponse
{
    public int TournamentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<MatchResponse> InitialMatches { get; set; } = [];
}

public class MatchResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TournamentId { get; set; }
    public int? FirstPlayerId { get; set; }
    public string? FirstPlayerUsername { get; set; }
    public int? SecondPlayerId { get; set; }
    public string? SecondPlayerUsername { get; set; }
    public int? WinnerId { get; set; }
    public string? WinnerUsername { get; set; }
    public DateTime MatchTime { get; set; }
}