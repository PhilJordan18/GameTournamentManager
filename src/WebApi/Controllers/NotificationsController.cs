using System.IdentityModel.Tokens.Jwt;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
[ApiController]
[Route("[controller]")]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpPost("subscribe")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
            await service.SubscribeToNotificationsAsync(userId, request.FcmToken);
            return Ok(new SubscribeResponse { Message = "Abonnement aux notifications réussi." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}