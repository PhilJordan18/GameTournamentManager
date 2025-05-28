namespace Core.DTOs.Auth;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class RegisterRequest
{
    public required string Firstname { get; set; }
    public required string Lastname { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string PhoneNumber { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
    public string Message { get; set; }
}