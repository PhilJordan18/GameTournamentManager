using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Persistence.Context;
using Core.DTOs.Auth;
using Core.Entities;
using Core.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class AuthServices(AppDbContext context, IConfiguration configuration) : IAuthServices
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if ( await context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse { Message = "L'email est déjà utilisé" };
        }
        
        if ( await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return new AuthResponse { Message = "Le nom d'utilisateur est déjà utilisé" };
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            Username = request.Username,
            Firstname = request.Firstname,
            Lastname = request.Lastname,
            Email = request.Email,
            Password = hashedPassword,
            PhoneNumber = request.PhoneNumber,
            RoleId = 2,
            Is2FAActived = false,
            TwoFactorSecretKey = string.Empty,
            TwoFactorRecoveryCodes = string.Empty
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();
        var user = await context.Users.Include(r => r.Role).FirstOrDefaultAsync(u => u.Id == newUser.Id);
        var token = GenerateToken(user!);

        return new AuthResponse { Token = token, Message = "Inscription effectuée avec succès!" };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await context.Users.Include(r => r.Role).FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return new AuthResponse { Message = "Email ou mot de passe incorrect." };

        var token = GenerateToken(user);
        return new AuthResponse { Token = token, Message = "Connection réussie !" };
    }

    public Task<AuthResponse> LogoutAsync()
    {
        return Task.FromResult(new AuthResponse
        {
            Token = string.Empty,
            Message = "Déconnection réussie"
        });
    }

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(int.Parse(configuration["Jwt:ExpireInMinutes"] ?? string.Empty)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}