using System.Net.Http.Json;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces.Tournament;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Client;

public class GameLoaderClient(AppDbContext context, IHttpClientFactory client, IConfiguration configuration, ILogger<GameLoaderClient> logger) : IGameLoaderClient
{
    private readonly HttpClient _client = client.CreateClient("RawgApi");

    private readonly string _apiKey = configuration["RawgApi:Key"] ?? throw new InvalidOperationException("La clé est introuvable");
    
    public async Task<int> LoadGameAsync(int pageSize = 20, int maxPage = 5)
    {
        int gamesAdded = 0;

        try
        {
            for (var page = 0; page < maxPage; page++)
            {
                var response = await _client.GetAsync($"games?key={_apiKey}&page={page}&page_size={pageSize}&tags=multiplayer");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var games = json.GetProperty("results").EnumerateArray();

                foreach (var game in games)
                {
                    var externalId = game.GetProperty("id").GetInt32().ToString();
                    var name = game.GetProperty("name").GetString() ?? string.Empty;

                    if (await context.Games.AnyAsync(g => g.ExternalId == externalId || g.Name == name))
                        continue;

                    var description = game.TryGetProperty("description_raw", out var desc) ? desc.GetString() ?? string.Empty : string.Empty;

                    var newGame = new Game
                    {
                        ExternalId = externalId,
                        Name = name,
                        Description = description,
                        Rules = "Règles standard du jeu (à personnaliser)."
                    };

                    context.Games.Add(newGame);
                    gamesAdded++;
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Page {Page} : {GamesAdded} jeux ajoutés.", page, gamesAdded);
            }
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Erreur lors de la récupération des jeux depuis RAWG.");
            throw;
        }

        return gamesAdded;
    }
}