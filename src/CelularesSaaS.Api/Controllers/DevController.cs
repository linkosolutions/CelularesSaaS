using Microsoft.AspNetCore.Mvc;

namespace CelularesSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    [HttpGet("hash")]
    public IActionResult GenerarHash([FromQuery] string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return Ok(new { hash });
    }
}