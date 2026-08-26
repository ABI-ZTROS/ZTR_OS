using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuraController : ControllerBase
{
    private readonly AuraLighting _auraLighting;
    private readonly ILogger<AuraController> _logger;

    public AuraController(AuraLighting auraLighting, ILogger<AuraController> logger)
    {
        _auraLighting = auraLighting;
        _logger = logger;
    }

    [HttpGet("modes")]
    [ProducesResponseType<ApiResponse<IEnumerable<string>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<string>>> ListModes()
    {
        var modes = Enum.GetNames(typeof(AuraMode));
        return Ok(new ApiResponse<IEnumerable<string>>(true, modes));
    }

    [HttpPost("apply")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> Apply([FromBody] SetAuraModeRequest request)
    {
        var result = _auraLighting.SetMode(request.Mode, request.Zone, request.R, request.G, request.B);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to apply Aura mode {request.Mode}"));
        }

        return Ok(new ApiResponse(true));
    }
}