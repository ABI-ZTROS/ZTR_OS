using System.Collections.Concurrent;

namespace ZTR.HAL;

/// <summary>
/// Detects sensor failures including stale data, out-of-range values, and communication errors.
/// Provides fallback to last known good values with automatic recovery when sensors come back online.
/// </summary>
public class SensorDegradationHandler
{
    private readonly ConcurrentDictionary<string, SensorHealthInfo> _sensorHealth;
    private readonly ConcurrentDictionary<string, double> _lastKnownGoodValues;
    private readonly ConcurrentDictionary<string, double> _minRanges;
    private readonly ConcurrentDictionary<string, double> _maxRanges;
    private readonly int _maxStaleSeconds;
    private readonly int _consecutiveFailureThreshold;

    /// <summary>
    /// Creates a new instance of the <see cref="SensorDegradationHandler"/> class.
    /// </summary>
    /// <param name="maxStaleSeconds">
    /// The maximum age of a sensor reading in seconds before it is considered stale. Default is 5 seconds.
    /// </param>
    /// <param name="consecutiveFailureThreshold">
    /// The number of consecutive failures before a sensor is marked as unhealthy. Default is 3.
    /// </param>
    public SensorDegradationHandler(int maxStaleSeconds = 5, int consecutiveFailureThreshold = 3)
    {
        _maxStaleSeconds = maxStaleSeconds;
        _consecutiveFailureThreshold = consecutiveFailureThreshold;
        _sensorHealth = new ConcurrentDictionary<string, SensorHealthInfo>();
        _lastKnownGoodValues = new ConcurrentDictionary<string, double>();
        _minRanges = new ConcurrentDictionary<string, double>();
        _maxRanges = new ConcurrentDictionary<string, double>();
    }

    /// <summary>
    /// Registers a sensor with valid range bounds for out-of-range detection.
    /// </summary>
    /// <param name="sensorName">The unique name of the sensor.</param>
    /// <param name="minValue">The minimum valid value.</param>
    /// <param name="maxValue">The maximum valid value.</param>
    public void RegisterSensor(string sensorName, double minValue = double.MinValue, double maxValue = double.MaxValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        _minRanges[sensorName] = minValue;
        _maxRanges[sensorName] = maxValue;

        if (!_sensorHealth.ContainsKey(sensorName))
        {
            _sensorHealth[sensorName] = new SensorHealthInfo
            {
                SensorName = sensorName,
                IsHealthy = true,
                LastHealthyTimestamp = DateTime.UtcNow,
                ConsecutiveFailures = 0
            };
        }
    }

    /// <summary>
    /// Reports a successful sensor reading, updating the last known good value and health status.
    /// </summary>
    /// <param name="sensorName">The sensor that reported a reading.</param>
    /// <param name="value">The reading value.</param>
    /// <param name="timestamp">The timestamp of the reading. Defaults to UTC now.</param>
    public void ReportSuccess(string sensorName, double value, DateTime? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        var ts = timestamp ?? DateTime.UtcNow;
        _lastKnownGoodValues[sensorName] = value;

        var info = _sensorHealth.GetOrAdd(sensorName, _ => new SensorHealthInfo
        {
            SensorName = sensorName,
            IsHealthy = true,
            LastHealthyTimestamp = ts,
            ConsecutiveFailures = 0
        });

        lock (info)
        {
            if (!info.IsHealthy)
            {
                info.IsHealthy = true;
                info.RecoveredTimestamp = ts;
                OnSensorRecovered?.Invoke(this, sensorName);
            }
            info.LastHealthyTimestamp = ts;
            info.ConsecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Reports a sensor failure, incrementing the consecutive failure counter.
    /// When the threshold is reached, the sensor is marked as unhealthy.
    /// </summary>
    /// <param name="sensorName">The sensor that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    public void ReportFailure(string sensorName, string reason = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        var info = _sensorHealth.GetOrAdd(sensorName, _ => new SensorHealthInfo
        {
            SensorName = sensorName,
            IsHealthy = true,
            LastHealthyTimestamp = DateTime.UtcNow,
            ConsecutiveFailures = 0
        });

        lock (info)
        {
            info.ConsecutiveFailures++;
            info.LastFailureTimestamp = DateTime.UtcNow;
            info.LastFailureReason = reason;

            if (info.ConsecutiveFailures >= _consecutiveFailureThreshold && info.IsHealthy)
            {
                info.IsHealthy = false;
                OnSensorFailed?.Invoke(this, sensorName);
            }
        }
    }

    /// <summary>
    /// Checks whether a sensor is currently healthy.
    /// </summary>
    /// <param name="sensorName">The sensor to check.</param>
    /// <returns>True if the sensor is healthy; otherwise false.</returns>
    public bool IsSensorHealthy(string sensorName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        if (_sensorHealth.TryGetValue(sensorName, out var info))
        {
            lock (info)
            {
                if (!info.IsHealthy)
                    return false;

                var age = DateTime.UtcNow - info.LastHealthyTimestamp;
                if (age.TotalSeconds > _maxStaleSeconds)
                    return false;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the last known good value for a sensor.
    /// </summary>
    /// <param name="sensorName">The sensor name.</param>
    /// <returns>The fallback value, or null if no good value is available.</returns>
    public double? GetFallbackValue(string sensorName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);
        return _lastKnownGoodValues.TryGetValue(sensorName, out var value) ? value : null;
    }

    /// <summary>
    /// Validates whether a sensor value is within the registered valid range.
    /// </summary>
    /// <param name="sensorName">The sensor name.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is within range; otherwise false.</returns>
    public bool IsValueInRange(string sensorName, double value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        if (_minRanges.TryGetValue(sensorName, out var min) &&
            _maxRanges.TryGetValue(sensorName, out var max))
        {
            return value >= min && value <= max;
        }

        return true;
    }

    /// <summary>
    /// Validates a sensor reading, reporting success or failure accordingly.
    /// Returns the value if valid, or the fallback value if invalid.
    /// </summary>
    /// <param name="sensorName">The sensor name.</param>
    /// <param name="value">The reading value.</param>
    /// <param name="timestamp">The timestamp of the reading.</param>
    /// <returns>The validated value, or the fallback value if the reading is invalid.</returns>
    public double ValidateReading(string sensorName, double value, DateTime? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        if (!IsValueInRange(sensorName, value))
        {
            ReportFailure(sensorName, $"Value {value} out of range");
            return GetFallbackValue(sensorName) ?? 0;
        }

        var ts = timestamp ?? DateTime.UtcNow;
        var age = DateTime.UtcNow - ts;
        if (age.TotalSeconds > _maxStaleSeconds)
        {
            ReportFailure(sensorName, $"Stale reading (age: {age.TotalSeconds:F1}s)");
            return GetFallbackValue(sensorName) ?? 0;
        }

        ReportSuccess(sensorName, value, ts);
        return value;
    }

    /// <summary>
    /// Gets the health information for all registered sensors.
    /// </summary>
    /// <returns>A read-only dictionary of sensor health statuses.</returns>
    public IReadOnlyDictionary<string, SensorHealthInfo> GetAllHealthInfo()
    {
        return _sensorHealth;
    }

    /// <summary>
    /// Gets the health information for a specific sensor.
    /// </summary>
    /// <param name="sensorName">The sensor name.</param>
    /// <returns>The health information, or null if the sensor is not registered.</returns>
    public SensorHealthInfo? GetSensorHealthInfo(string sensorName)
    {
        return _sensorHealth.TryGetValue(sensorName, out var info) ? info : null;
    }

    /// <summary>
    /// Forces a sensor to be marked as healthy (manual recovery).
    /// </summary>
    /// <param name="sensorName">The sensor to recover.</param>
    public void ForceRecover(string sensorName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        var info = _sensorHealth.GetOrAdd(sensorName, _ => new SensorHealthInfo
        {
            SensorName = sensorName,
            IsHealthy = true,
            LastHealthyTimestamp = DateTime.UtcNow,
            ConsecutiveFailures = 0
        });

        lock (info)
        {
            info.IsHealthy = true;
            info.ConsecutiveFailures = 0;
            info.LastHealthyTimestamp = DateTime.UtcNow;
            info.RecoveredTimestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Raised when a sensor transitions from healthy to unhealthy.
    /// </summary>
    public event EventHandler<string>? OnSensorFailed;

    /// <summary>
    /// Raised when a sensor recovers from an unhealthy state.
    /// </summary>
    public event EventHandler<string>? OnSensorRecovered;

    /// <summary>
    /// Resets all sensor health tracking data.
    /// </summary>
    public void Reset()
    {
        _sensorHealth.Clear();
        _lastKnownGoodValues.Clear();
    }
}

/// <summary>
/// Represents the health status of a single sensor.
/// </summary>
public class SensorHealthInfo
{
    /// <summary>
    /// Gets or sets the sensor name.
    /// </summary>
    public string SensorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the sensor is currently healthy.
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp of the last healthy reading.
    /// </summary>
    public DateTime LastHealthyTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last failure.
    /// </summary>
    public DateTime? LastFailureTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the reason for the last failure.
    /// </summary>
    public string LastFailureReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the sensor last recovered.
    /// </summary>
    public DateTime? RecoveredTimestamp { get; set; }
}
