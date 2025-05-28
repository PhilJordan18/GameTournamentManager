using Core.DTOs.Auth;
using Core.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthServices services) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await services.RegisterAsync(request);
        if (response.Token == null)
            return BadRequest(new AuthResponse { Message = response.Message });
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await services.LoginAsync(request);
        if (response.Token == null)
            return BadRequest(new AuthResponse { Message = response.Message });
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var response = await services.LogoutAsync();
        return Ok(response);
    }
}