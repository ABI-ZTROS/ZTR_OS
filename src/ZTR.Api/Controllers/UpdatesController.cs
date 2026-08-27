using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UpdatesController : ControllerBase
{
    private readonly BiosUpdateChecker _checker;
    private readonly DeviceProbe _deviceProbe;
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(BiosUpdateChecker checker, DeviceProbe deviceProbe, ILogger<UpdatesController> logger)
    {
        _checker = checker;
        _deviceProbe = deviceProbe;
        _logger = logger;
    }

    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<object>>> CheckAsync([FromQuery] string? model = null)
    {
        try
        {
            string modelName = model ?? _deviceProbe.DetectModel() ?? "Unknown ASUS Device";
            var updates = await _checker.CheckForUpdatesAsync(modelName);
            return Ok(new ApiResponse<object>(true, new { model = modelName, updates }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Update check failed");
            return Ok(new ApiResponse<object>(false, new { error = ex.Message }));
        }
    }
}
