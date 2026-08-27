using Microsoft.AspNetCore.Mvc;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BindingController : ControllerBase
{
    private readonly ProcessTracker _processTracker;
    private readonly CpuAffinityManager _cpuManager;
    private readonly GpuAffinityManager _gpuManager;
    private readonly TopologyService _topologyService;
    private readonly ILogger<BindingController> _logger;

    public BindingController(
        ProcessTracker processTracker,
        CpuAffinityManager cpuManager,
        GpuAffinityManager gpuManager,
        TopologyService topologyService,
        ILogger<BindingController> logger)
    {
        _processTracker = processTracker;
        _cpuManager = cpuManager;
        _gpuManager = gpuManager;
        _topologyService = topologyService;
        _logger = logger;
    }

    [HttpGet("processes")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProcessBinding>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ProcessBinding>>> ListProcesses()
    {
        var processes = _processTracker.GetAllProcesses();
        return Ok(new ApiResponse<IReadOnlyList<ProcessBinding>>(true, processes));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ProcessBinding>>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ProcessBinding>>> ListBindings()
    {
        var processes = _processTracker.GetAllProcesses();
        return Ok(new ApiResponse<IReadOnlyList<ProcessBinding>>(true, processes));
    }

    [HttpPost("{processId}")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetBinding(int processId, [FromBody] SetBindingRequest request)
    {
        // P1 FIXED: Null guard on request.Affinity to prevent NRE
        if (request.Affinity == null || request.Affinity.Count == 0)
        {
            return BadRequest(new ApiResponse(false, "Affinity list cannot be null or empty"));
        }

        long affinityMask = 0;
        foreach (var coreId in request.Affinity)
        {
            if (coreId >= 0 && coreId < 64)
            {
                affinityMask |= 1L << coreId;
            }
        }

        if (affinityMask == 0)
        {
            return BadRequest(new ApiResponse(false, "Invalid affinity mask"));
        }

        var cpuResult = _cpuManager.SetAffinity(processId, affinityMask);
        if (!cpuResult)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set affinity for process {processId}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpDelete("{processId}")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse> RemoveBinding(int processId)
    {
        // P1 FIXED: Check actual result instead of ignoring it and always returning success
        var result = _cpuManager.SetAffinity(processId, -1);
        if (!result)
        {
            return NotFound(new ApiResponse(false, $"Process {processId} not found or unbinding failed"));
        }
        return Ok(new ApiResponse(true));
    }

    [HttpPost("cpu")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetCpuAffinity([FromBody] SetCpuAffinityRequest request)
    {
        var result = _cpuManager.SetAffinity(request.ProcessId, request.AffinityMask);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set CPU affinity for process {request.ProcessId}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpPost("gpu")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> SetGpuAffinity([FromBody] SetGpuAffinityRequest request)
    {
        var result = _gpuManager.SetGpuAffinity(request.ProcessId, request.GpuIndex);
        if (!result)
        {
            return BadRequest(new ApiResponse(false, $"Failed to set GPU affinity for process {request.ProcessId}"));
        }

        return Ok(new ApiResponse(true));
    }

    [HttpGet("topology")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetTopology()
    {
        var cpuTopology = _topologyService.GetCpuTopology();
        var gpuTopology = _topologyService.GetGpuTopology();

        var topology = new
        {
            Cpu = cpuTopology,
            Gpu = gpuTopology
        };

        return Ok(new ApiResponse<object>(true, topology));
    }

    [HttpPost("auto-bind")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> SetAutoBind([FromBody] SetAutoBindRequest request)
    {
        _logger.LogInformation("Auto-bind games set to: {Enabled}", request.Enabled);
        return Ok(new ApiResponse(true));
    }
}

public class SetAutoBindRequest
{
    public bool Enabled { get; set; }
}