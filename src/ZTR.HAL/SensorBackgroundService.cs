using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Background service for periodic sensor data collection using the <see cref="SensorPipeline"/>.
/// Integrates with the .NET <see cref="BackgroundService"/> infrastructure for seamless
/// lifecycle management within an <see cref="IHostedService"/> environment.
/// </summary>
public class SensorBackgroundService : BackgroundService
{
    private readonly SensorPipeline _pipeline;
    private readonly SensorQueue _queue;
    private readonly ILogger<SensorBackgroundService> _logger;
    private int _intervalMs = 1000;

    /// <summary>
    /// Gets or sets the polling interval in milliseconds.
    /// Valid range is 100ms to 5000ms. Default is 1000ms.
    /// </summary>
    public int IntervalMs
    {
        get => _intervalMs;
        set => _intervalMs = Math.Clamp(value, 100, 5000);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SensorBackgroundService"/> class.
    /// </summary>
    /// <param name="pipeline">The sensor pipeline for data collection.</param>
    /// <param name="queue">The sensor queue for storing collected data.</param>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    public SensorBackgroundService(
        SensorPipeline pipeline,
        SensorQueue queue,
        ILogger<SensorBackgroundService> logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor Background Service started. Polling interval: {IntervalMs}ms", _intervalMs);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var state = _pipeline.CollectOnce();
                _queue.Enqueue(state);
                _logger.LogDebug("Collected sensor state: CPU={CpuTemp}°C, GPU={GpuTemp}°C",
                    state.Cpu.Temperature, state.Gpu.Temperature);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sensor Background Service cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sensor data collection");
            }
        }

        _logger.LogInformation("Sensor Background Service stopped.");
    }

    /// <summary>
    /// Gets the latest hardware state from the queue.
    /// </summary>
    /// <returns>The latest <see cref="HardwareState"/>, or null if no data is available.</returns>
    public HardwareState? GetLatestState()
    {
        var history = _queue.GetHistory(1);
        return history.Count > 0 ? history[0] : null;
    }

    /// <summary>
    /// Gets a specified number of recent hardware states.
    /// </summary>
    /// <param name="count">The number of recent states to retrieve.</param>
    /// <returns>A read-only list of recent hardware states.</returns>
    public IReadOnlyList<HardwareState> GetRecentStates(int count = 100)
    {
        return _queue.GetHistory(count);
    }
}
