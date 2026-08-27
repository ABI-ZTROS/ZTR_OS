using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class BatteryControlTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<BatteryControl>> _mockLogger;
    private AsusAcpi _acpi;
    private BatteryControl _control;
    private bool _disposed;

    public BatteryControlTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockLogger = new Mock<ILogger<BatteryControl>>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new BatteryControl(_acpi, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockLogger.Reset();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new BatteryControl(_acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    #region SetChargeLimit Tests

    [Fact]
    public void SetChargeLimit_60Percent_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargeLimit(60);

        Assert.True(result);
    }

    [Fact]
    public void SetChargeLimit_80Percent_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargeLimit(80);

        Assert.True(result);
    }

    [Fact]
    public void SetChargeLimit_100Percent_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargeLimit(100);

        Assert.True(result);
    }

    [Fact]
    public void SetChargeLimit_InvalidPercent_ReturnsFalse()
    {
        bool result = _control.SetChargeLimit(50);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_70Percent_ReturnsFalse()
    {
        bool result = _control.SetChargeLimit(70);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_90Percent_ReturnsFalse()
    {
        bool result = _control.SetChargeLimit(90);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_ZeroPercent_ReturnsFalse()
    {
        bool result = _control.SetChargeLimit(0);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_NegativePercent_ReturnsFalse()
    {
        bool result = _control.SetChargeLimit(-10);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetChargeLimit(80);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(Array.Empty<byte>());

        bool result = _control.SetChargeLimit(80);

        Assert.False(result);
    }

    [Fact]
    public void SetChargeLimit_LogsWarning_ForInvalidPercent()
    {
        _control.SetChargeLimit(50);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetChargeLimit_LogsInformation_WhenSuccessful()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetChargeLimit(80);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetChargeLimit_LogsWarning_WhenAcpiFailsAfterException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Hardware error"));

        bool result = _control.SetChargeLimit(80);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetChargeLimit Tests

    [Fact]
    public void GetChargeLimit_ReturnsValueFromAcpi()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(80));

        int result = _control.GetChargeLimit();

        Assert.Equal(80, result);
    }

    [Fact]
    public void GetChargeLimit_Returns60_WhenAcpiReturns60()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(60));

        int result = _control.GetChargeLimit();

        Assert.Equal(60, result);
    }

    [Fact]
    public void GetChargeLimit_Returns100_WhenAcpiReturns100()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(100));

        int result = _control.GetChargeLimit();

        Assert.Equal(100, result);
    }

    [Fact]
    public void GetChargeLimit_WhenDeviceUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetChargeLimit();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetChargeLimit_OnBufferException_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Read error"));

        int result = _control.GetChargeLimit();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetChargerMode Tests

    [Fact]
    public void SetChargerMode_ACOnly_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargerMode(ChargerMode.ACOnly);

        Assert.True(result);
    }

    [Fact]
    public void SetChargerMode_BatteryOnly_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargerMode(ChargerMode.BatteryOnly);

        Assert.True(result);
    }

    [Fact]
    public void SetChargerMode_Both_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetChargerMode(ChargerMode.Both);

        Assert.True(result);
    }

    [Fact]
    public void SetChargerMode_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetChargerMode(ChargerMode.ACOnly);

        Assert.False(result);
    }

    [Fact]
    public void SetChargerMode_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(Array.Empty<byte>());

        bool result = _control.SetChargerMode(ChargerMode.ACOnly);

        Assert.False(result);
    }

    [Fact]
    public void SetChargerMode_LogsInformation()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetChargerMode(ChargerMode.Both);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetChargerMode Tests

    [Fact]
    public void GetChargerMode_ReturnsACOnly()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.Equal(ChargerMode.ACOnly, _control.GetChargerMode());
    }

    [Fact]
    public void GetChargerMode_ReturnsBatteryOnly()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.Equal(ChargerMode.BatteryOnly, _control.GetChargerMode());
    }

    [Fact]
    public void GetChargerMode_ReturnsBoth()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        Assert.Equal(ChargerMode.Both, _control.GetChargerMode());
    }

    [Fact]
    public void GetChargerMode_WhenDeviceUnavailable_ReturnsBoth()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.Equal(ChargerMode.Both, _control.GetChargerMode());
    }

    [Fact]
    public void GetChargerMode_WhenInvalidValue_ReturnsBoth()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(99));

        Assert.Equal(ChargerMode.Both, _control.GetChargerMode());
    }

    #endregion

    #region GetBatteryInfo Tests

    [Fact]
    public void GetBatteryInfo_ReturnsChargeLimit()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(80));

        var info = _control.GetBatteryInfo();

        Assert.Equal(80, info.ChargeLimit);
    }

    [Fact]
    public void GetBatteryInfo_DefaultChargeLimit_WhenUnavailable()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        var info = _control.GetBatteryInfo();

        Assert.Equal(100, info.ChargeLimit);
    }

    [Fact]
    public void GetBatteryInfo_IsCharging_WhenModeNotBatteryOnly()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        var info = _control.GetBatteryInfo();

        Assert.True(info.IsCharging);
    }

    [Fact]
    public void GetBatteryInfo_NotCharging_WhenModeBatteryOnly()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        var info = _control.GetBatteryInfo();

        Assert.False(info.IsCharging);
    }

    [Fact]
    public void GetBatteryInfo_StatusString_WhenIdle()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        var info = _control.GetBatteryInfo();

        Assert.Equal("Idle", info.Status);
    }

    [Fact]
    public void GetBatteryInfo_StatusString_WhenCharging()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        var info = _control.GetBatteryInfo();

        Assert.Equal("Charging", info.Status);
    }

    [Fact]
    public void GetBatteryInfo_StatusString_WhenDischarging()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        var info = _control.GetBatteryInfo();

        Assert.Equal("Discharging", info.Status);
    }

    [Fact]
    public void GetBatteryInfo_StatusString_WhenUnknown()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(99));

        var info = _control.GetBatteryInfo();

        Assert.Equal("Unknown", info.Status);
    }

    [Fact]
    public void GetBatteryInfo_ReturnsDefaultStatus_WhenAcpiFails()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        var info = _control.GetBatteryInfo();

        Assert.NotNull(info);
    }

    #endregion

    #region GetBatteryHealth Tests

    [Fact]
    public void GetBatteryHealth_Returns100_WhenDeviceAvailable()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        int health = _control.GetBatteryHealth();

        Assert.Equal(100, health);
    }

    [Fact]
    public void GetBatteryHealth_ReturnsNegativeOne_WhenUnavailable()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int health = _control.GetBatteryHealth();

        Assert.Equal(-1, health);
    }

    [Fact]
    public void GetBatteryHealth_ReturnsNegativeOne_OnException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Error"));

        int health = _control.GetBatteryHealth();

        Assert.Equal(-1, health);
    }

    #endregion

    #region SetDischargeLevel Tests

    [Fact]
    public void SetDischargeLevel_ValidLevel_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetDischargeLevel(50);

        Assert.True(result);
    }

    [Fact]
    public void SetDischargeLevel_Zero_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetDischargeLevel(0);

        Assert.True(result);
    }

    [Fact]
    public void SetDischargeLevel_100_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetDischargeLevel(100);

        Assert.True(result);
    }

    [Fact]
    public void SetDischargeLevel_Negative_ReturnsFalse()
    {
        bool result = _control.SetDischargeLevel(-5);

        Assert.False(result);
    }

    [Fact]
    public void SetDischargeLevel_Above100_ReturnsFalse()
    {
        bool result = _control.SetDischargeLevel(101);

        Assert.False(result);
    }

    [Fact]
    public void SetDischargeLevel_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetDischargeLevel(50);

        Assert.False(result);
    }

    [Fact]
    public void SetDischargeLevel_LogsWarning_ForInvalidLevel()
    {
        _control.SetDischargeLevel(-1);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetDischargeLevel_LogsInformation_WhenSuccessful()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetDischargeLevel(30);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAcpi_DoesNotThrow()
    {
        var logger = new Mock<ILogger<BatteryControl>>();
        Assert.NotNull(new BatteryControl(_acpi, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullAcpi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BatteryControl(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new BatteryControl(_acpi));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        _control.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        _control.Dispose();
        _control.Dispose();
        Assert.True(true);
    }

    #endregion

    #region BatteryInfo Tests

    [Fact]
    public void BatteryInfo_DefaultValues()
    {
        var info = new BatteryInfo();

        Assert.Equal(0, info.ChargePercent);
        Assert.False(info.IsCharging);
        Assert.Equal(0, info.ChargeLimit);
        Assert.Equal(string.Empty, info.Status);
        Assert.Equal(100, info.HealthPercent);
    }

    [Fact]
    public void BatteryInfo_SetProperties()
    {
        var info = new BatteryInfo
        {
            ChargePercent = 75,
            IsCharging = true,
            ChargeLimit = 80,
            Status = "Charging",
            HealthPercent = 95
        };

        Assert.Equal(75, info.ChargePercent);
        Assert.True(info.IsCharging);
        Assert.Equal(80, info.ChargeLimit);
        Assert.Equal("Charging", info.Status);
        Assert.Equal(95, info.HealthPercent);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SetAndGetChargeLimit_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(80));

        bool setResult = _control.SetChargeLimit(80);
        int getResult = _control.GetChargeLimit();

        Assert.True(setResult);
        Assert.Equal(80, getResult);
    }

    [Fact]
    public void SetAndGetChargerMode_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)ChargerMode.BatteryOnly));

        bool setResult = _control.SetChargerMode(ChargerMode.BatteryOnly);
        var getResult = _control.GetChargerMode();

        Assert.True(setResult);
        Assert.Equal(ChargerMode.BatteryOnly, getResult);
    }

    [Fact]
    public void AllChargeLimits_AreValidated()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        Assert.True(_control.SetChargeLimit(60));
        Assert.True(_control.SetChargeLimit(80));
        Assert.True(_control.SetChargeLimit(100));
    }

    #endregion
}