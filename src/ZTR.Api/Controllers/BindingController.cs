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
}