using Core.DTOs.Auth;
using Core.Entities;
using Infrastructure.Persistence.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests;

public class AuthServicesTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Roles.Add(new Role { Id = 2, Name = "User" });
        context.SaveChanges();
        return context;
    }

    private static IConfiguration CreateMockedConfig()
    {
        var config = new Dictionary<string, string>
        {
            ["Jwt:Key"] = "X3tXrmw1f9AmOvCArOFehttRdvyPPLfURMIj9Am4pV6sdVczlgLUMpytU4cH8uNY",
            ["Jwt:Issuer"] = "GameTournamentManager",
            ["Jwt:Audience"] = "GameTournamentManager",
            ["Jwt:ExpireInMinutes"] = "60"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(config).Build();
    }

    [Fact]
    public async Task RegisterUserSuccessfully()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        var request = new RegisterRequest
        {
            Firstname = "John",
            Lastname = "Doe",
            Username = "johndoe",
            Email = "john@example.com",
            Password = "Test1234!",
            PhoneNumber = "1234567890"
        };

        var response = await services.RegisterAsync(request);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal("Inscription effectuée avec succès!", response.Message);
    }

    [Fact]
    public async Task RegisterUserWithExistingEmail()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        context.Users.Add(new User
        {
            Username = "otheruser",
            Email = "john@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
            RoleId = 2,
            Firstname = "john",
            Lastname = "doe",
            PhoneNumber = "30382920",
            Is2FAActived = false,
            TwoFactorSecretKey = string.Empty,
            TwoFactorRecoveryCodes = string.Empty        
        });
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Firstname = "John",
            Lastname = "Doe",
            Username = "johndoe",
            Email = "john@example.com",
            Password = "Test1234!",
            PhoneNumber = "1234567890"
        };

        var response = await services.RegisterAsync(request);
        Assert.Null(response.Token);
        Assert.Equal("L'email est déjà utilisé", response.Message);
    }

    [Fact]
    public async Task RegisterUserWithExistingUsername()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        context.Users.Add(new User
        {
            Username = "johndoe",
            Email = "other@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
            RoleId = 2,
            Firstname = "john",
            Lastname = "doe",
            PhoneNumber = "30382920",
            Is2FAActived = false,
            TwoFactorSecretKey = string.Empty,
            TwoFactorRecoveryCodes = string.Empty
        });
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Firstname = "John",
            Lastname = "Doe",
            Username = "johndoe",
            Email = "john@example.com",
            Password = "Test1234!",
            PhoneNumber = "1234567890"
        };

        var response = await services.RegisterAsync(request);
        Assert.Null(response.Token);
        Assert.Equal("Le nom d'utilisateur est déjà utilisé", response.Message);
    }

    [Fact]
    public async Task LoginUserSuccessfully()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        context.Users.Add(new User
        {
            Username = "johndoe",
            Email = "john@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            RoleId = 2,
            Firstname = "john",
            Lastname = "doe",
            PhoneNumber = "30382920",
            Is2FAActived = false,
            TwoFactorSecretKey = string.Empty,
            TwoFactorRecoveryCodes = string.Empty
        });
        await context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "CorrectPassword"
        };

        var response = await services.LoginAsync(request);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal("Connection réussie !", response.Message);
    }

    [Fact]
    public async Task LoginUserWithWrongPassword()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        context.Users.Add(new User
        {
            Username = "johndoe",
            Email = "john@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            RoleId = 2,
            Firstname = "john",
            Lastname = "doe",
            PhoneNumber = "30382920",
            Is2FAActived = false,
            TwoFactorSecretKey = string.Empty,
            TwoFactorRecoveryCodes = string.Empty
        });
        await context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        var response = await services.LoginAsync(request);
        Assert.Null(response.Token);
        Assert.Equal("Email ou mot de passe incorrect.", response.Message);
    }

    [Fact]
    public async Task LoginUserWithNonExistingEmail()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        var request = new LoginRequest
        {
            Email = "unknown@example.com",
            Password = "DoesNotMatter"
        };

        var response = await services.LoginAsync(request);
        Assert.Null(response.Token);
        Assert.Equal("Email ou mot de passe incorrect.", response.Message);
    }

    [Fact]
    public async Task LogoutUserSuccessfully()
    {
        var context = CreateDbContext();
        var config = CreateMockedConfig();
        var services = new AuthServices(context, config);

        var response = await services.LogoutAsync();
        Assert.Equal(string.Empty, response.Token);
        Assert.Equal("Déconnection réussie", response.Message);
    }
}
