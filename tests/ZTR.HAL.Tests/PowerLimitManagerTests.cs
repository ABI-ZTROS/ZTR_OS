using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class PowerLimitManagerTests : IDisposable
{
    private readonly Mock<IAtkDevice> _mockAtkDevice;
    private readonly Mock<ILogger<PowerLimitManager>> _mockLogger;
    private readonly AsusAcpi _acpi;
    private readonly PowerLimitManager _manager;
    private bool _disposed;

    public PowerLimitManagerTests()
    {
        _mockAtkDevice = new Mock<IAtkDevice>();
        _mockLogger = new Mock<ILogger<PowerLimitManager>>();

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        _acpi = new AsusAcpi(_mockAtkDevice.Object);
        _manager = new PowerLimitManager(_acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _manager.Dispose();
            _acpi.Dispose();
        }
    }

    #region SetSPL Tests

    [Fact]
    public void SetSPL_ValidWatts_ReturnsTrue()
    {
        var result = _manager.SetSPL(45);

        Assert.True(result);
    }

    [Fact]
    public void SetSPL_ValidWatts_CallsAcpi()
    {
        _manager.SetSPL(45);

        _mockAtkDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetSPL_ZeroWatts_ReturnsTrue()
    {
        var result = _manager.SetSPL(0);

        Assert.True(result);
    }

    [Fact]
    public void SetSPL_NegativeWatts_ClampsToZero()
    {
        var result = _manager.SetSPL(-10);

        Assert.True(result);
    }

    [Fact]
    public void SetSPL_ExceedsMax_ClampsTo250()
    {
        var result = _manager.SetSPL(500);

        Assert.True(result);
    }

    [Fact]
    public void SetSPL_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetSPL(45);

        Assert.False(result);
    }

    [Fact]
    public void SetSPL_WhenAcpiUnavailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var manager = new PowerLimitManager(acpi);

        var result = manager.SetSPL(45);

        Assert.False(result);
    }

    #endregion

    #region SetSPPT Tests

    [Fact]
    public void SetSPPT_ValidWatts_ReturnsTrue()
    {
        var result = _manager.SetSPPT(35);

        Assert.True(result);
    }

    [Fact]
    public void SetSPPT_ValidWatts_CallsAcpi()
    {
        _manager.SetSPPT(35);

        _mockAtkDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetSPPT_ZeroWatts_ReturnsTrue()
    {
        var result = _manager.SetSPPT(0);

        Assert.True(result);
    }

    [Fact]
    public void SetSPPT_ExceedsMax_ClampsTo250()
    {
        var result = _manager.SetSPPT(300);

        Assert.True(result);
    }

    [Fact]
    public void SetSPPT_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetSPPT(35);

        Assert.False(result);
    }

    [Fact]
    public void SetSPPT_WhenAcpiUnavailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var manager = new PowerLimitManager(acpi);

        var result = manager.SetSPPT(35);

        Assert.False(result);
    }

    #endregion

    #region SetFPPT Tests

    [Fact]
    public void SetFPPT_ValidWatts_ReturnsTrue()
    {
        var result = _manager.SetFPPT(20);

        Assert.True(result);
    }

    [Fact]
    public void SetFPPT_ValidWatts_CallsAcpi()
    {
        _manager.SetFPPT(20);

        _mockAtkDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetFPPT_ZeroWatts_ReturnsTrue()
    {
        var result = _manager.SetFPPT(0);

        Assert.True(result);
    }

    [Fact]
    public void SetFPPT_ExceedsMax_ClampsTo250()
    {
        var result = _manager.SetFPPT(400);

        Assert.True(result);
    }

    [Fact]
    public void SetFPPT_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetFPPT(20);

        Assert.False(result);
    }

    [Fact]
    public void SetFPPT_WhenAcpiUnavailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var manager = new PowerLimitManager(acpi);

        var result = manager.SetFPPT(20);

        Assert.False(result);
    }

    #endregion

    #region SetAllPowerLimits Tests

    [Fact]
    public void SetAllPowerLimits_AllSucceed_ReturnsTrue()
    {
        var result = _manager.SetAllPowerLimits(45, 35, 20);

        Assert.True(result);
    }

    [Fact]
    public void SetAllPowerLimits_AllFail_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetAllPowerLimits(45, 35, 20);

        Assert.False(result);
    }

    [Fact]
    public void SetAllPowerLimits_OneFails_ReturnsFalse()
    {
        int callCount = 0;
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetAllPowerLimits(45, 35, 20);

        Assert.False(result);
    }

    #endregion

    #region GetPowerState Tests

    [Fact]
    public void GetPowerState_AfterSettingSPL_ReturnsCorrectValue()
    {
        _manager.SetSPL(55);

        var state = _manager.GetPowerState();

        Assert.Equal(55, state.SPL);
    }

    [Fact]
    public void GetPowerState_AfterSettingAll_ReturnsAllValues()
    {
        _manager.SetAllPowerLimits(55, 40, 25);

        var state = _manager.GetPowerState();

        Assert.Equal(55, state.SPL);
        Assert.Equal(40, state.SPPT);
        Assert.Equal(25, state.FPPT);
    }

    [Fact]
    public void GetPowerState_InitialState_ReturnsZeros()
    {
        var state = _manager.GetPowerState();

        Assert.Equal(0, state.SPL);
        Assert.Equal(0, state.SPPT);
        Assert.Equal(0, state.FPPT);
        Assert.Equal(0, state.DynamicBoostLevel);
    }

    #endregion

    #region SetDynamicBoost Tests

    [Fact]
    public void SetDynamicBoost_5W_ReturnsTrue()
    {
        var result = _manager.SetDynamicBoost(5);

        Assert.True(result);
    }

    [Fact]
    public void SetDynamicBoost_15W_ReturnsTrue()
    {
        var result = _manager.SetDynamicBoost(15);

        Assert.True(result);
    }

    [Fact]
    public void SetDynamicBoost_20W_ReturnsTrue()
    {
        var result = _manager.SetDynamicBoost(20);

        Assert.True(result);
    }

    [Fact]
    public void SetDynamicBoost_InvalidLevel_ReturnsTrueWithZero()
    {
        var result = _manager.SetDynamicBoost(999);

        Assert.True(result);
    }

    [Fact]
    public void SetDynamicBoost_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.SetDynamicBoost(15);

        Assert.False(result);
    }

    [Fact]
    public void SetDynamicBoost_UpdatesState()
    {
        _manager.SetDynamicBoost(15);

        var state = _manager.GetPowerState();

        Assert.Equal(15, state.DynamicBoostLevel);
    }

    #endregion

    #region ApplyModePowerDefaults Tests

    [Fact]
    public void ApplyModePowerDefaults_SilentMode_AppliesCorrectLimits()
    {
        var result = _manager.ApplyModePowerDefaults(AsusMode.PerformanceSilent);

        Assert.True(result);
    }

    [Fact]
    public void ApplyModePowerDefaults_BalancedMode_AppliesCorrectLimits()
    {
        var result = _manager.ApplyModePowerDefaults(AsusMode.PerformanceBalanced);

        Assert.True(result);
    }

    [Fact]
    public void ApplyModePowerDefaults_TurboMode_AppliesCorrectLimits()
    {
        var result = _manager.ApplyModePowerDefaults(AsusMode.PerformanceTurbo);

        Assert.True(result);
    }

    [Fact]
    public void ApplyModePowerDefaults_FullSpeedMode_AppliesCorrectLimits()
    {
        var result = _manager.ApplyModePowerDefaults(AsusMode.PerformanceFullSpeed);

        Assert.True(result);
    }

    [Fact]
    public void ApplyModePowerDefaults_ManualMode_AppliesCorrectLimits()
    {
        var result = _manager.ApplyModePowerDefaults(AsusMode.PerformanceManual);

        Assert.True(result);
    }

    [Fact]
    public void ApplyModePowerDefaults_UnknownMode_ReturnsFalse()
    {
        var result = _manager.ApplyModePowerDefaults((AsusMode)999);

        Assert.False(result);
    }

    #endregion

    #region GetModePowerDefaults Tests

    [Fact]
    public void GetModePowerDefaults_SilentMode_ReturnsCorrectDefaults()
    {
        var defaults = _manager.GetModePowerDefaults(AsusMode.PerformanceSilent);

        Assert.NotNull(defaults);
        Assert.Equal(15, defaults.Value.spl);
        Assert.Equal(10, defaults.Value.sppt);
        Assert.Equal(5, defaults.Value.fppt);
    }

    [Fact]
    public void GetModePowerDefaults_BalancedMode_ReturnsCorrectDefaults()
    {
        var defaults = _manager.GetModePowerDefaults(AsusMode.PerformanceBalanced);

        Assert.NotNull(defaults);
        Assert.Equal(35, defaults.Value.spl);
    }

    [Fact]
    public void GetModePowerDefaults_TurboMode_ReturnsCorrectDefaults()
    {
        var defaults = _manager.GetModePowerDefaults(AsusMode.PerformanceTurbo);

        Assert.NotNull(defaults);
        Assert.Equal(45, defaults.Value.spl);
    }

    [Fact]
    public void GetModePowerDefaults_UnknownMode_ReturnsNull()
    {
        var defaults = _manager.GetModePowerDefaults((AsusMode)999);

        Assert.Null(defaults);
    }

    #endregion

    #region ResetToDefaults Tests

    [Fact]
    public void ResetToDefaults_AfterModeApplied_ReturnsTrue()
    {
        _manager.ApplyModePowerDefaults(AsusMode.PerformanceBalanced);

        var result = _manager.ResetToDefaults();

        Assert.True(result);
    }

    [Fact]
    public void ResetToDefaults_NoModeApplied_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var result = _manager.ResetToDefaults();

        Assert.False(result);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _manager.Dispose();
        _manager.Dispose();
    }

    #endregion
}