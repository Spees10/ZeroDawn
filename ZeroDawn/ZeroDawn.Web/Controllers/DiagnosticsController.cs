#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace ZeroDawn.Web.Controllers;

[ApiController]
[Route("api/debug")]
public class DiagnosticsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public DiagnosticsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("throw")]
    public IActionResult Throw()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Debug exception test.");
    }
}
