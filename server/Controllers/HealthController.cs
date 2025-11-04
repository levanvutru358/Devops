using Microsoft.AspNetCore.Mvc;

namespace EmoApi.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Root() => Ok(new { status = "ok" });

    // Explicit health endpoint for compose and CI checks
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "ok" });
}
