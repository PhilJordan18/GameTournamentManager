namespace Core.Entities;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int NbMaxPlayers { get; set; }
    public DateTime BeginningDate { get; set; }
    public DateTime EndDate { get; set; }
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public int CreatorId { get; set; } 
    public User Creator { get; set; } = null!; 
    public ICollection<TournamentPlayer> Players { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
    public string Description { get; set; } = string.Empty;
}


public class Status
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TournamentPlayer
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }

    public DateTime JoinedAt { get; set; }
}


public class Match
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public int FirstPlayerId { get; set; }
    public User FirstPlayer { get; set; } = null!;
    public int? SecondPlayerId { get; set; } 
    public User? SecondPlayer { get; set; }
    public int? PendingWinnerId { get; set; } 
    public User? PendingWinner { get; set; }
    public int? WinnerId { get; set; } 
    public User? Winner { get; set; }
    public DateTime MatchTime { get; set; }
}


public class Game
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Rules { get; set; }
    public string ExternalId { get; set; } = string.Empty;
}

//dotnet ef migrations add AddExternalIdToGame --project src/Infrastructure/ --startup-project src/WebApi/
// dotnet ef database update --project src/Infrastructure/ --startup-project src/WebApi/