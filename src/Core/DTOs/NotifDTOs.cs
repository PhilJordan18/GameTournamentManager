namespace Core.DTOs;

public class SubscribeRequest
{
    public string FcmToken { get; set; } = string.Empty;
}

public class SubscribeResponse
{
    public string Message { get; set; } = string.Empty;
}