using System.Net.Http.Headers;
using System.Text;
using Core.Interfaces.Auth;
using Core.Interfaces.Tournament;
using Infrastructure.Services;
using Core.Validators.Auth;
using Core.Validators.Tournament;
using FluentValidation;
using Infrastructure.Client;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Infrastructure;

public static class DependencyInjections
{
    public static IServiceCollection AddInfrastructures(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Api");
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty))
            };
        });

        services.AddScoped<IAuthServices, AuthServices>();
        services.AddValidatorsFromAssemblyContaining<LoginValidator>();
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
        
        services.AddHttpClient("RawgApi", client =>
        {
            client.BaseAddress = new Uri(configuration["RawgApi:BaseUrl"]!);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddScoped<ITournamentServices, TournamentServices>();
        services.AddValidatorsFromAssemblyContaining<TournamentConfigValidator>();
        services.AddScoped<IGameLoaderClient, GameLoaderClient>();
        
        return services;
    }
}