using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class ModeControlTests : IDisposable
{
    private readonly Mock<IAtkDevice> _mockAtkDevice;
    private readonly Mock<ILogger<ModeControl>> _mockLogger;
    private readonly AsusAcpi _acpi;
    private readonly PowerLimitManager _powerManager;
    private readonly ModeControl _modeControl;
    private bool _disposed;

    public ModeControlTests()
    {
        _mockAtkDevice = new Mock<IAtkDevice>();
        _mockLogger = new Mock<ILogger<ModeControl>>();

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        _acpi = new AsusAcpi(_mockAtkDevice.Object);
        _powerManager = new PowerLimitManager(_acpi);
        _modeControl = new ModeControl(_acpi, _powerManager, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _modeControl.Dispose();
            _powerManager.Dispose();
            _acpi.Dispose();
        }
    }

    #region SetMode Tests

    [Fact]
    public void SetMode_Silent_Succeeds()
    {
        var result = _modeControl.SetMode(AsusMode.PerformanceSilent);

        Assert.True(result);
    }

    [Fact]
    public void SetMode_Balanced_Succeeds()
    {
        var result = _modeControl.SetMode(AsusMode.PerformanceBalanced);

        Assert.True(result);
    }

    [Fact]
    public void SetMode_Turbo_Succeeds()
    {
        var result = _modeControl.SetMode(AsusMode.PerformanceTurbo);

        Assert.True(result);
    }

    [Fact]
    public void SetMode_FullSpeed_Succeeds()
    {
        var result = _modeControl.SetMode(AsusMode.PerformanceFullSpeed);

        Assert.True(result);
    }

    [Fact]
    public void SetMode_Manual_Succeeds()
    {
        var result = _modeControl.SetMode(AsusMode.PerformanceManual);

        Assert.True(result);
    }

    [Fact]
    public void SetMode_SetsCurrentMode()
    {
        _modeControl.SetMode(AsusMode.PerformanceTurbo);

        Assert.Equal(AsusMode.PerformanceTurbo, _modeControl.CurrentMode);
    }

    [Fact]
    public void SetMode_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);

        var result = _modeControl.SetMode(AsusMode.PerformanceTurbo);

        Assert.False(result);
    }

    [Fact]
    public void SetMode_WhenAcpiUnavailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var powerMgr = new PowerLimitManager(acpi);
        using var modeCtrl = new ModeControl(acpi, powerMgr);

        var result = modeCtrl.SetMode(AsusMode.PerformanceTurbo);

        Assert.False(result);
    }

    [Fact]
    public void SetMode_AppliesDefaultFanCurves_ForNonManualMode()
    {
        _modeControl.SetMode(AsusMode.PerformanceTurbo);

        _mockAtkDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetMode_SwitchBetweenModes_UpdatesMode()
    {
        _modeControl.SetMode(AsusMode.PerformanceSilent);
        Assert.Equal(AsusMode.PerformanceSilent, _modeControl.CurrentMode);

        _modeControl.SetMode(AsusMode.PerformanceTurbo);
        Assert.Equal(AsusMode.PerformanceTurbo, _modeControl.CurrentMode);

        _modeControl.SetMode(AsusMode.PerformanceFullSpeed);
        Assert.Equal(AsusMode.PerformanceFullSpeed, _modeControl.CurrentMode);
    }

    #endregion

    #region GetCurrentMode Tests

    [Fact]
    public void GetCurrentMode_ReadsFromAcpi()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)AsusMode.PerformanceTurbo));

        var mode = _modeControl.GetCurrentMode();

        Assert.Equal(AsusMode.PerformanceTurbo, mode);
    }

    [Fact]
    public void GetCurrentMode_WhenInvalidValue_ReturnsCached()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(999));

        var mode = _modeControl.GetCurrentMode();

        Assert.Equal(_modeControl.CurrentMode, mode);
    }

    [Fact]
    public void GetCurrentMode_AfterSetMode_ReturnsCorrectValue()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)AsusMode.PerformanceBalanced));

        _modeControl.SetMode(AsusMode.PerformanceBalanced);
        var mode = _modeControl.GetCurrentMode();

        Assert.Equal(AsusMode.PerformanceBalanced, mode);
    }

    #endregion

    #region SetCpuFanCurve Tests

    [Fact]
    public void SetCpuFanCurve_ValidCurve_ReturnsTrue()
    {
        var curve = CreateTestCurve();

        var result = _modeControl.SetCpuFanCurve(curve);

        Assert.True(result);
    }

    [Fact]
    public void SetCpuFanCurve_ValidCurve_CallsAcpi()
    {
        var curve = CreateTestCurve();

        _modeControl.SetCpuFanCurve(curve);

        _mockAtkDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetCpuFanCurve_NullCurve_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _modeControl.SetCpuFanCurve(null!));
    }

    [Fact]
    public void SetCpuFanCurve_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);
        var curve = CreateTestCurve();

        var result = _modeControl.SetCpuFanCurve(curve);

        Assert.False(result);
    }

    #endregion

    #region SetGpuFanCurve Tests

    [Fact]
    public void SetGpuFanCurve_ValidCurve_ReturnsTrue()
    {
        var curve = CreateTestCurve();

        var result = _modeControl.SetGpuFanCurve(curve);

        Assert.True(result);
    }

    [Fact]
    public void SetGpuFanCurve_NullCurve_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _modeControl.SetGpuFanCurve(null!));
    }

    [Fact]
    public void SetGpuFanCurve_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);
        var curve = CreateTestCurve();

        var result = _modeControl.SetGpuFanCurve(curve);

        Assert.False(result);
    }

    #endregion

    #region SetMidFanCurve Tests

    [Fact]
    public void SetMidFanCurve_ValidCurve_ReturnsTrue()
    {
        var curve = CreateTestCurve();

        var result = _modeControl.SetMidFanCurve(curve);

        Assert.True(result);
    }

    [Fact]
    public void SetMidFanCurve_NullCurve_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _modeControl.SetMidFanCurve(null!));
    }

    [Fact]
    public void SetMidFanCurve_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);
        var curve = CreateTestCurve();

        var result = _modeControl.SetMidFanCurve(curve);

        Assert.False(result);
    }

    #endregion

    #region GetFanCurve Tests

    [Fact]
    public void GetFanCurve_CPU_ReturnsCurve()
    {
        var curve = CreateTestCurve();
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(FanCurveCalculator.CurveToBytes(curve));

        var result = _modeControl.GetFanCurve(AsusFan.CPU);

        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void GetFanCurve_GPU_ReturnsCurve()
    {
        var curve = CreateTestCurve();
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(FanCurveCalculator.CurveToBytes(curve));

        var result = _modeControl.GetFanCurve(AsusFan.GPU);

        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void GetFanCurve_Mid_ReturnsCurve()
    {
        var curve = CreateTestCurve();
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(FanCurveCalculator.CurveToBytes(curve));

        var result = _modeControl.GetFanCurve(AsusFan.Mid);

        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void GetFanCurve_WhenReadFails_ReturnsCached()
    {
        var curve = CreateTestCurve();
        _modeControl.SetCpuFanCurve(curve);

        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _modeControl.GetFanCurve(AsusFan.CPU);

        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void GetFanCurve_UnknownFan_ReturnsEmptyFallback()
    {
        var result = _modeControl.GetFanCurve(AsusFan.XGM);

        Assert.NotNull(result);
    }

    #endregion

    #region SetPowerLimits Tests

    [Fact]
    public void SetPowerLimits_ValidValues_ReturnsTrue()
    {
        var result = _modeControl.SetPowerLimits(45, 35, 20);

        Assert.True(result);
    }

    [Fact]
    public void SetPowerLimits_ValidValues_UpdatesState()
    {
        _modeControl.SetPowerLimits(45, 35, 20);

        var (spl, sppt, fppt) = _modeControl.GetPowerLimits();
        Assert.Equal(45, spl);
        Assert.Equal(35, sppt);
        Assert.Equal(20, fppt);
    }

    [Fact]
    public void SetPowerLimits_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);

        var result = _modeControl.SetPowerLimits(45, 35, 20);

        Assert.False(result);
    }

    #endregion

    #region GetPowerLimits Tests

    [Fact]
    public void GetPowerLimits_AfterSet_ReturnsCorrectValues()
    {
        _modeControl.SetPowerLimits(55, 40, 25);

        var (spl, sppt, fppt) = _modeControl.GetPowerLimits();

        Assert.Equal(55, spl);
        Assert.Equal(40, sppt);
        Assert.Equal(25, fppt);
    }

    [Fact]
    public void GetPowerLimits_InitialState_ReturnsZeros()
    {
        var (spl, sppt, fppt) = _modeControl.GetPowerLimits();

        Assert.Equal(0, spl);
        Assert.Equal(0, sppt);
        Assert.Equal(0, fppt);
    }

    #endregion

    #region SetCpuTempLimit Tests

    [Fact]
    public void SetCpuTempLimit_ValidTemp_ReturnsTrue()
    {
        var result = _modeControl.SetCpuTempLimit(90);

        Assert.True(result);
    }

    [Fact]
    public void SetCpuTempLimit_ValidTemp_UpdatesState()
    {
        _modeControl.SetCpuTempLimit(95);

        Assert.Equal(95, _modeControl.CpuTempLimit);
    }

    [Fact]
    public void SetCpuTempLimit_BelowMin_ClampsTo60()
    {
        var result = _modeControl.SetCpuTempLimit(30);

        Assert.True(result);
    }

    [Fact]
    public void SetCpuTempLimit_AboveMax_ClampsTo110()
    {
        var result = _modeControl.SetCpuTempLimit(150);

        Assert.True(result);
    }

    [Fact]
    public void SetCpuTempLimit_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);

        var result = _modeControl.SetCpuTempLimit(90);

        Assert.False(result);
    }

    #endregion

    #region AutoApplySettings Tests

    [Fact]
    public void AutoApplySettings_AfterSettingCurves_ReturnsTrue()
    {
        var curve = CreateTestCurve();
        _modeControl.SetCpuFanCurve(curve);
        _modeControl.SetGpuFanCurve(curve);

        var result = _modeControl.AutoApplySettings();

        Assert.True(result);
    }

    [Fact]
    public void AutoApplySettings_WithPowerLimits_ReturnsTrue()
    {
        _modeControl.SetPowerLimits(45, 35, 20);

        var result = _modeControl.AutoApplySettings();

        Assert.True(result);
    }

    [Fact]
    public void AutoApplySettings_WithTempLimit_ReturnsTrue()
    {
        _modeControl.SetCpuTempLimit(90);

        var result = _modeControl.AutoApplySettings();

        Assert.True(result);
    }

    [Fact]
    public void AutoApplySettings_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(false);

        var result = _modeControl.AutoApplySettings();

        Assert.False(result);
    }

    [Fact]
    public void AutoApplySettings_NoSettings_ReturnsTrue()
    {
        var result = _modeControl.AutoApplySettings();

        Assert.True(result);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _modeControl.Dispose();
        _modeControl.Dispose();
    }

    #endregion

    #region Helper Methods

    private static FanCurvePoint[] CreateTestCurve()
    {
        return new[]
        {
            new FanCurvePoint { Temperature = 30, Speed = 0 },
            new FanCurvePoint { Temperature = 40, Speed = 10 },
            new FanCurvePoint { Temperature = 50, Speed = 25 },
            new FanCurvePoint { Temperature = 60, Speed = 50 },
            new FanCurvePoint { Temperature = 70, Speed = 60 },
            new FanCurvePoint { Temperature = 80, Speed = 75 },
            new FanCurvePoint { Temperature = 90, Speed = 90 },
            new FanCurvePoint { Temperature = 100, Speed = 100 }
        };
    }

    #endregion
}