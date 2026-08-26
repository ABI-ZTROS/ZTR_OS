using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessController : ControllerBase
{
    private readonly ProcessTracker _processTracker;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(
        ProcessTracker processTracker,
        ILogger<ProcessController> logger)
    {
        _processTracker = processTracker;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProcessBinding>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ProcessBinding>>> ListProcesses()
    {
        var processes = _processTracker.GetAllProcesses();
        return Ok(new ApiResponse<IReadOnlyList<ProcessBinding>>(true, processes));
    }

    [HttpGet("foreground")]
    [ProducesResponseType<ApiResponse<ProcessBinding>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ProcessBinding>>(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<ProcessBinding>> GetForegroundProcess()
    {
        var process = _processTracker.GetForegroundProcess();
        if (process == null)
        {
            return NotFound(new ApiResponse<ProcessBinding>(false, null, "No foreground process detected"));
        }

        return Ok(new ApiResponse<ProcessBinding>(true, process));
    }

    [HttpGet("gpu-intensive")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProcessBinding>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ProcessBinding>>> GetGpuIntensiveProcesses()
    {
        var processes = _processTracker.GetGpuIntensiveProcesses();
        return Ok(new ApiResponse<IReadOnlyList<ProcessBinding>>(true, processes));
    }
}