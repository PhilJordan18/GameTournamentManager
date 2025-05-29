namespace Core.Entities;

public class SubmitMatchResultRequest
{
    public int WinnerId { get; set; } 
}

public class SubmitMatchResultResponse
{
    public int MatchId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ValidateMatchResultRequest
{
    public bool IsApproved { get; set; } 
}

public class ValidateMatchResultResponse
{
    public int MatchId { get; set; }
    public string Message { get; set; } = string.Empty;
}