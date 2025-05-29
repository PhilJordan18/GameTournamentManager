using System.IdentityModel.Tokens.Jwt;
using Core.DTOs;
using Core.Entities;
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

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Join([FromBody] int tournamentId, int id)
    {
        var response = await services.StartTournamentAsync(tournamentId, id);
        return Ok(response);
    }


    [HttpPut("{id:int}/join")]
    [Authorize]
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
    
    [HttpPost("{id:int}/matches/{matchId:int}/result")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMatchResult(int id, int matchId, [FromBody] SubmitMatchResultRequest request)
    {
        var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var response = await services.SubmitMatchResultAsync(id, matchId, request, userId);
        return Ok(response);
    }

    [HttpPut("{id:int}/matches/{matchId:int}/validate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateMatchResult(int id, int matchId, [FromBody] ValidateMatchResultRequest request)
    {
        var adminId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var response = await services.ValidateMatchResultAsync(id, matchId, request, adminId);
        return Ok(response);
    }

    [HttpPost("{id:int}/advance")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdvanceTournament(int id)
    {
        var adminId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        await services.AdvanceTournamentAsync(id, adminId);
        return Ok(new { Message = "Tournoi avancé avec succès." });
    }
}