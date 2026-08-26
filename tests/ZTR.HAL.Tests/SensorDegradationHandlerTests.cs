using ZTR.HAL;

namespace ZTR.HAL.Tests;

public class SensorDegradationHandlerTests
{
    private readonly SensorDegradationHandler _handler;

    public SensorDegradationHandlerTests()
    {
        _handler = new SensorDegradationHandler(maxStaleSeconds: 5, consecutiveFailureThreshold: 3);
    }

    #region RegisterSensor Tests

    [Fact]
    public void RegisterSensor_ValidName_DoesNotThrow()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        Assert.True(true);
    }

    [Fact]
    public void RegisterSensor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.RegisterSensor(""));
    }

    [Fact]
    public void RegisterSensor_WhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.RegisterSensor("   "));
    }

    [Fact]
    public void RegisterSensor_DuplicateName_DoesNotThrow()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        Assert.True(true);
    }

    #endregion

    #region ReportSuccess Tests

    [Fact]
    public void ReportSuccess_SetsHealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ReportSuccess_StoresLastKnownGoodValue()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        var fallback = _handler.GetFallbackValue("CPU Temperature");
        Assert.Equal(45, fallback);
    }

    [Fact]
    public void ReportSuccess_ResetsFailureCount()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportSuccess("CPU Temperature", 45);

        var info = _handler.GetSensorHealthInfo("CPU Temperature");
        Assert.NotNull(info);
        Assert.Equal(0, info!.ConsecutiveFailures);
    }

    [Fact]
    public void ReportSuccess_TriggersRecoveryEvent()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        string? recoveredSensor = null;
        _handler.OnSensorRecovered += (_, name) => recoveredSensor = name;

        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportSuccess("CPU Temperature", 45);

        Assert.Equal("CPU Temperature", recoveredSensor);
    }

    [Fact]
    public void ReportSuccess_UnhealthySensor_TriggersRecovery()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        Assert.False(_handler.IsSensorHealthy("CPU Temperature"));

        _handler.ReportSuccess("CPU Temperature", 45);

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ReportSuccess_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.ReportSuccess("", 45));
    }

    #endregion

    #region ReportFailure Tests

    [Fact]
    public void ReportFailure_IncrementsFailureCount()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        var info = _handler.GetSensorHealthInfo("CPU Temperature");
        Assert.Equal(2, info!.ConsecutiveFailures);
    }

    [Fact]
    public void ReportFailure_ReachesThreshold_MarksUnhealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        Assert.False(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ReportFailure_BelowThreshold_RemainsHealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ReportFailure_TriggersFailureEvent()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        string? failedSensor = null;
        _handler.OnSensorFailed += (_, name) => failedSensor = name;

        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        Assert.Equal("CPU Temperature", failedSensor);
    }

    [Fact]
    public void ReportFailure_RecordsReason()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature", "Timeout");

        var info = _handler.GetSensorHealthInfo("CPU Temperature");
        Assert.Equal("Timeout", info!.LastFailureReason);
    }

    [Fact]
    public void ReportFailure_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.ReportFailure(""));
    }

    #endregion

    #region IsSensorHealthy Tests

    [Fact]
    public void IsSensorHealthy_RegisteredHealthy_ReturnsTrue()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void IsSensorHealthy_NotRegistered_ReturnsFalse()
    {
        Assert.False(_handler.IsSensorHealthy("Unknown Sensor"));
    }

    [Fact]
    public void IsSensorHealthy_Unhealthy_ReturnsFalse()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        Assert.False(_handler.IsSensorHealthy("CPU Temperature"));
    }

    #endregion

    #region GetFallbackValue Tests

    [Fact]
    public void GetFallbackValue_HasValue_ReturnsValue()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        var fallback = _handler.GetFallbackValue("CPU Temperature");
        Assert.Equal(45, fallback);
    }

    [Fact]
    public void GetFallbackValue_NoValue_ReturnsNull()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        var fallback = _handler.GetFallbackValue("CPU Temperature");
        Assert.Null(fallback);
    }

    [Fact]
    public void GetFallbackValue_NotRegistered_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.GetFallbackValue(""));
    }

    #endregion

    #region IsValueInRange Tests

    [Fact]
    public void IsValueInRange_ValueInRange_ReturnsTrue()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        Assert.True(_handler.IsValueInRange("CPU Temperature", 45));
    }

    [Fact]
    public void IsValueInRange_ValueAboveMax_ReturnsFalse()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        Assert.False(_handler.IsValueInRange("CPU Temperature", 150));
    }

    [Fact]
    public void IsValueInRange_ValueBelowMin_ReturnsFalse()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        Assert.False(_handler.IsValueInRange("CPU Temperature", -10));
    }

    [Fact]
    public void IsValueInRange_NoBounds_ReturnsTrue()
    {
        _handler.RegisterSensor("Unbounded Sensor");

        Assert.True(_handler.IsValueInRange("Unbounded Sensor", 99999));
    }

    #endregion

    #region ValidateReading Tests

    [Fact]
    public void ValidateReading_ValidValue_ReturnsValue()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        var result = _handler.ValidateReading("CPU Temperature", 45);

        Assert.Equal(45, result);
        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ValidateReading_OutOfRange_UsesFallback()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        var result = _handler.ValidateReading("CPU Temperature", 200);

        Assert.Equal(45, result);
    }

    [Fact]
    public void ValidateReading_OutOfRange_NoFallback_ReturnsZero()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        var result = _handler.ValidateReading("CPU Temperature", 200);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ValidateReading_StaleData_UsesFallback()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        var staleTimestamp = DateTime.UtcNow.AddSeconds(-10);
        var result = _handler.ValidateReading("CPU Temperature", 50, staleTimestamp);

        Assert.Equal(45, result);
    }

    #endregion

    #region ForceRecover Tests

    [Fact]
    public void ForceRecover_MarksHealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");

        _handler.ForceRecover("CPU Temperature");

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void ForceRecover_ResetsFailureCount()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportFailure("CPU Temperature");

        _handler.ForceRecover("CPU Temperature");

        var info = _handler.GetSensorHealthInfo("CPU Temperature");
        Assert.Equal(0, info!.ConsecutiveFailures);
    }

    [Fact]
    public void ForceRecover_NotRegistered_CreatesNewEntry()
    {
        _handler.ForceRecover("New Sensor");

        Assert.True(_handler.IsSensorHealthy("New Sensor"));
    }

    #endregion

    #region GetAllHealthInfo Tests

    [Fact]
    public void GetAllHealthInfo_NoSensors_ReturnsEmpty()
    {
        var info = _handler.GetAllHealthInfo();
        Assert.Empty(info);
    }

    [Fact]
    public void GetAllHealthInfo_WithSensors_ReturnsAll()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.RegisterSensor("GPU Temperature", 0, 110);

        var info = _handler.GetAllHealthInfo();
        Assert.Equal(2, info.Count);
    }

    #endregion

    #region GetSensorHealthInfo Tests

    [Fact]
    public void GetSensorHealthInfo_ExistingSensor_ReturnsInfo()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        var info = _handler.GetSensorHealthInfo("CPU Temperature");
        Assert.NotNull(info);
        Assert.Equal("CPU Temperature", info!.SensorName);
    }

    [Fact]
    public void GetSensorHealthInfo_NonExisting_ReturnsNull()
    {
        var info = _handler.GetSensorHealthInfo("NonExisting");
        Assert.Null(info);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllData()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        _handler.Reset();

        Assert.Empty(_handler.GetAllHealthInfo());
        Assert.Null(_handler.GetFallbackValue("CPU Temperature"));
    }

    #endregion

    #region HealthInfo Model Tests

    [Fact]
    public void SensorHealthInfo_DefaultValues()
    {
        var info = new SensorHealthInfo();

        Assert.Equal(string.Empty, info.SensorName);
        Assert.True(info.IsHealthy);
        Assert.True(DateTime.UtcNow >= info.LastHealthyTimestamp.AddSeconds(-1));
        Assert.Equal(0, info.ConsecutiveFailures);
        Assert.Null(info.LastFailureTimestamp);
        Assert.Equal(string.Empty, info.LastFailureReason);
        Assert.Null(info.RecoveredTimestamp);
    }

    [Fact]
    public void SensorHealthInfo_SetProperties()
    {
        var now = DateTime.UtcNow;
        var info = new SensorHealthInfo
        {
            SensorName = "CPU Temperature",
            IsHealthy = false,
            LastHealthyTimestamp = now,
            ConsecutiveFailures = 5,
            LastFailureTimestamp = now,
            LastFailureReason = "Timeout",
            RecoveredTimestamp = now
        };

        Assert.Equal("CPU Temperature", info.SensorName);
        Assert.False(info.IsHealthy);
        Assert.Equal(5, info.ConsecutiveFailures);
        Assert.Equal("Timeout", info.LastFailureReason);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        var handler = new SensorDegradationHandler();
        Assert.NotNull(handler);
    }

    [Fact]
    public void Constructor_CustomThresholds_CreatesInstance()
    {
        var handler = new SensorDegradationHandler(maxStaleSeconds: 10, consecutiveFailureThreshold: 5);
        Assert.NotNull(handler);
    }

    #endregion

    #region Integration: Sensor Lifecycle Tests

    [Fact]
    public void SensorLifecycle_NormalOperation_StaysHealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);

        for (int i = 0; i < 100; i++)
        {
            _handler.ReportSuccess("CPU Temperature", 40 + i * 0.1);
        }

        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
        Assert.NotNull(_handler.GetFallbackValue("CPU Temperature"));
    }

    [Fact]
    public void SensorLifecycle_TransientFailure_AutoRecovers()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        _handler.ReportFailure("CPU Temperature");
        _handler.ReportFailure("CPU Temperature");
        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));

        _handler.ReportFailure("CPU Temperature");
        Assert.False(_handler.IsSensorHealthy("CPU Temperature"));

        _handler.ReportSuccess("CPU Temperature", 46);
        Assert.True(_handler.IsSensorHealthy("CPU Temperature"));
    }

    [Fact]
    public void SensorLifecycle_PersistentFailure_StaysUnhealthy()
    {
        _handler.RegisterSensor("CPU Temperature", 0, 110);
        _handler.ReportSuccess("CPU Temperature", 45);

        for (int i = 0; i < 10; i++)
        {
            _handler.ReportFailure("CPU Temperature", "Persistent error");
        }

        Assert.False(_handler.IsSensorHealthy("CPU Temperature"));
        Assert.NotNull(_handler.GetFallbackValue("CPU Temperature"));
    }

    #endregion
}
