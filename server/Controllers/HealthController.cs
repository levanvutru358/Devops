using Microsoft.AspNetCore.Mvc;

namespace EmoApi.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Root() => Ok(new { status = "ok" });
}

