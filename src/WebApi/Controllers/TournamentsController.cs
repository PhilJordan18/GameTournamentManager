using System.IdentityModel.Tokens.Jwt;
using Core.DTOs;
using Core.Interfaces.Tournament;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
[ApiController]
[Route("[controller]")]

public class TournamentsController(ITournamentServices services) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] TournamentConfig config, int userId)
    {
        var response = await services.CreateTournamentAsync(config, userId);
        return Ok(response);
    }

    [HttpPut("{id:int}/start")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Start(int id)
    {
        var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var request = new JoinTournamentRequest { TournamentId = id };
        var response = await services.JoinTournamentAsync(request, userId);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll()
    {
        var tournaments = await services.GetAllTournamentsAsync();
        return Ok(tournaments);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTournament(int id)
    {
        var tournament = (await services.GetAllTournamentsAsync()).FirstOrDefault(t => t.Id == id);
        return Ok(tournament);
    }
}