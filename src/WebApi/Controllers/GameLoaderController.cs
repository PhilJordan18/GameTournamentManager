using Core.Interfaces.Tournament;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[ApiController]
[Route("games")]
public class GameLoaderController(IGameLoaderClient client, AppDbContext context) : ControllerBase
{
    [HttpPost("load")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoadGames([FromQuery] int pageSize = 20, [FromQuery] int maxPages = 5)
    {
        try
        {
            var gamesAdded = await client.LoadGameAsync(pageSize, maxPages);
            return Ok(new { Message = $"{gamesAdded} jeux chargés avec succès." });
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { Message = "Erreur lors du chargement des jeux.", Error = ex.Message });
        }
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllGames()
    {
        var games = await context.Games
            .Select(g => new
            {
                g.Name,
                g.Description,
                g.Rules
            })
            .ToListAsync();
        return Ok(games);
    }
}