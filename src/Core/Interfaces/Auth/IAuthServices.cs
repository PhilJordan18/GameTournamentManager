using Core.DTOs.Auth;

namespace Core.Interfaces.Auth;

public interface IAuthServices
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> LogoutAsync();
}