using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AllyControlTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<AllyControl>> _mockLogger;
    private Mock<IHidReportWriter> _mockWriter;
    private AsusAcpi _acpi;
    private AsusHid _hid;
    private AllyControl _control;
    private bool _disposed;

    public AllyControlTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockLogger = new Mock<ILogger<AllyControl>>();
        _mockWriter = new Mock<IHidReportWriter>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _hid = new AsusHid(_mockWriter.Object);
        _control = new AllyControl(_acpi, _hid, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockLogger.Reset();
        _mockWriter.Reset();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _hid = new AsusHid(_mockWriter.Object);
        _control = new AllyControl(_acpi, _hid, _mockLogger.Object);
    }

    private void RegisterAllMainPids()
    {
        foreach (var pid in AsusHid.MainAuraPids)
            _hid.RegisterStream(pid);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    #region GetSupportedFpsLimits Tests

    [Fact]
    public void GetSupportedFpsLimits_ReturnsExpectedLimits()
    {
        var limits = _control.GetSupportedFpsLimits();

        Assert.Contains(30, limits);
        Assert.Contains(40, limits);
        Assert.Contains(50, limits);
        Assert.Contains(60, limits);
        Assert.Contains(75, limits);
        Assert.Contains(90, limits);
        Assert.Contains(120, limits);
        Assert.Contains(240, limits);
    }

    [Fact]
    public void GetSupportedFpsLimits_ReturnsCorrectCount()
    {
        var limits = _control.GetSupportedFpsLimits();

        Assert.Equal(9, limits.Count);
    }

    [Fact]
    public void GetSupportedFpsLimits_AreAllPositive()
    {
        var limits = _control.GetSupportedFpsLimits();

        Assert.All(limits, l => Assert.True(l > 0));
    }

    #endregion

    #region SetControllerMode Tests

    [Fact]
    public void SetControllerMode_Auto_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetControllerMode(ControllerMode.Auto);

        Assert.True(result);
    }

    [Fact]
    public void SetControllerMode_Gamepad_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetControllerMode(ControllerMode.Gamepad);

        Assert.True(result);
    }

    [Fact]
    public void SetControllerMode_WASD_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetControllerMode(ControllerMode.WASD);

        Assert.True(result);
    }

    [Fact]
    public void SetControllerMode_Mouse_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetControllerMode(ControllerMode.Mouse);

        Assert.True(result);
    }

    [Fact]
    public void SetControllerMode_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.SetControllerMode(ControllerMode.Gamepad);

        Assert.False(result);
    }

    [Fact]
    public void SetControllerMode_LogsInformation()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetControllerMode(ControllerMode.Gamepad);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetControllerMode_LogsWarning_WhenHidFailsAfterException()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("HID error"));

        bool result = _control.SetControllerMode(ControllerMode.Gamepad);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetControllerMode Tests

    [Fact]
    public void GetControllerMode_ReturnsDefaultAuto()
    {
        var result = _control.GetControllerMode();

        Assert.Equal(ControllerMode.Auto, result);
    }

    [Fact]
    public void GetControllerMode_OnException_ReturnsAuto()
    {
        _mockWriter.Setup(w => w.ReadFeature(It.IsAny<byte>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Read error"));

        var result = _control.GetControllerMode();

        Assert.Equal(ControllerMode.Auto, result);
    }

    #endregion

    #region SetFpsLimit Tests

    [Fact]
    public void SetFpsLimit_30Fps_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetFpsLimit(30);

        Assert.True(result);
    }

    [Fact]
    public void SetFpsLimit_60Fps_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetFpsLimit(60);

        Assert.True(result);
    }

    [Fact]
    public void SetFpsLimit_120Fps_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetFpsLimit(120);

        Assert.True(result);
    }

    [Fact]
    public void SetFpsLimit_240Fps_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetFpsLimit(240);

        Assert.True(result);
    }

    [Fact]
    public void SetFpsLimit_UnsupportedFps_ReturnsFalse()
    {
        bool result = _control.SetFpsLimit(100);

        Assert.False(result);
    }

    [Fact]
    public void SetFpsLimit_500Fps_ReturnsFalse()
    {
        bool result = _control.SetFpsLimit(500);

        Assert.False(result);
    }

    [Fact]
    public void SetFpsLimit_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetFpsLimit(60);

        Assert.False(result);
    }

    [Fact]
    public void SetFpsLimit_LogsWarning_ForUnsupported()
    {
        _control.SetFpsLimit(100);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetFpsLimit_LogsInformation()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetFpsLimit(60);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetFpsLimit Tests

    [Fact]
    public void GetFpsLimit_ReturnsValue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(60));

        int result = _control.GetFpsLimit();

        Assert.Equal(60, result);
    }

    [Fact]
    public void GetFpsLimit_WhenUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetFpsLimit();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetFpsLimit_WhenAcpiFails_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _control.GetFpsLimit();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetAutoTDP Tests

    [Fact]
    public void SetAutoTDP_6W_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetAutoTDP(6);

        Assert.True(result);
    }

    [Fact]
    public void SetAutoTDP_15W_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetAutoTDP(15);

        Assert.True(result);
    }

    [Fact]
    public void SetAutoTDP_25W_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetAutoTDP(25);

        Assert.True(result);
    }

    [Fact]
    public void SetAutoTDP_Below6W_ReturnsFalse()
    {
        bool result = _control.SetAutoTDP(5);

        Assert.False(result);
    }

    [Fact]
    public void SetAutoTDP_Above25W_ReturnsFalse()
    {
        bool result = _control.SetAutoTDP(26);

        Assert.False(result);
    }

    [Fact]
    public void SetAutoTDP_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetAutoTDP(15);

        Assert.False(result);
    }

    [Fact]
    public void SetAutoTDP_LogsWarning_ForInvalid()
    {
        _control.SetAutoTDP(3);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAutoTDP Tests

    [Fact]
    public void GetAutoTDP_ReturnsValue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(15));

        int result = _control.GetAutoTDP();

        Assert.Equal(15, result);
    }

    [Fact]
    public void GetAutoTDP_WhenUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetAutoTDP();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetVibration Tests

    [Fact]
    public void SetVibration_ValidLevel_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetVibration(50);

        Assert.True(result);
    }

    [Fact]
    public void SetVibration_Zero_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetVibration(0);

        Assert.True(result);
    }

    [Fact]
    public void SetVibration_100_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetVibration(100);

        Assert.True(result);
    }

    [Fact]
    public void SetVibration_Negative_ReturnsFalse()
    {
        bool result = _control.SetVibration(-1);

        Assert.False(result);
    }

    [Fact]
    public void SetVibration_Above100_ReturnsFalse()
    {
        bool result = _control.SetVibration(101);

        Assert.False(result);
    }

    [Fact]
    public void SetVibration_LogsWarning_ForInvalid()
    {
        _control.SetVibration(-1);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetVibration Tests

    [Fact]
    public void GetVibration_WhenAvailable_ReturnsValue()
    {
        _mockWriter.Setup(w => w.ReadFeature(It.IsAny<byte>(), It.IsAny<int>()))
            .Returns(new byte[] { 128 });

        int result = _control.GetVibration();

        Assert.True(result >= 0);
    }

    [Fact]
    public void GetVibration_WhenUnavailable_ReturnsNegativeOne()
    {
        _mockWriter.Setup(w => w.ReadFeature(It.IsAny<byte>(), It.IsAny<int>()))
            .Returns((byte[]?)null);

        int result = _control.GetVibration();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetKeyMapping Tests

    [Fact]
    public void SetKeyMapping_ValidInput_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetKeyMapping(0, 65);

        Assert.True(result);
    }

    [Fact]
    public void SetKeyMapping_InvalidButtonId_ReturnsFalse()
    {
        bool result = _control.SetKeyMapping(-1, 65);

        Assert.False(result);
    }

    [Fact]
    public void SetKeyMapping_ButtonIdAbove15_ReturnsFalse()
    {
        bool result = _control.SetKeyMapping(16, 65);

        Assert.False(result);
    }

    [Fact]
    public void SetKeyMapping_InvalidKeyCode_ReturnsFalse()
    {
        bool result = _control.SetKeyMapping(0, -1);

        Assert.False(result);
    }

    [Fact]
    public void SetKeyMapping_KeyCodeAbove255_ReturnsFalse()
    {
        bool result = _control.SetKeyMapping(0, 256);

        Assert.False(result);
    }

    [Fact]
    public void SetKeyMapping_LogsWarning_ForInvalid()
    {
        _control.SetKeyMapping(-1, 65);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ResetKeyMappings Tests

    [Fact]
    public void ResetKeyMappings_Succeeds()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.ResetKeyMappings();

        Assert.True(result);
    }

    [Fact]
    public void ResetKeyMappings_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.ResetKeyMappings();

        Assert.False(result);
    }

    [Fact]
    public void ResetKeyMappings_WhenHidFails_ReturnsFalse()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("HID error"));

        bool result = _control.ResetKeyMappings();

        Assert.False(result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        var logger = new Mock<ILogger<AllyControl>>();
        Assert.NotNull(new AllyControl(_acpi, _hid, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullAcpi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AllyControl(null!, _hid));
    }

    [Fact]
    public void Constructor_WithNullHid_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AllyControl(_acpi, null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new AllyControl(_acpi, _hid));
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

    #region Integration Tests

    [Fact]
    public void SetAndGetControllerMode_RoundTrip()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();
        _mockWriter.Setup(w => w.ReadFeature(It.IsAny<byte>(), It.IsAny<int>()))
            .Returns(new byte[] { (byte)ControllerMode.Gamepad });

        bool setResult = _control.SetControllerMode(ControllerMode.Gamepad);

        Assert.True(setResult);
    }

    [Fact]
    public void SetAndGetFpsLimit_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(60));

        bool setResult = _control.SetFpsLimit(60);
        int getResult = _control.GetFpsLimit();

        Assert.True(setResult);
        Assert.Equal(60, getResult);
    }

    [Fact]
    public void SetAndGetAutoTDP_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(15));

        bool setResult = _control.SetAutoTDP(15);
        int getResult = _control.GetAutoTDP();

        Assert.True(setResult);
        Assert.Equal(15, getResult);
    }

    [Fact]
    public void AllFpsLimits_AreValidated()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        Assert.True(_control.SetFpsLimit(30));
        Assert.True(_control.SetFpsLimit(40));
        Assert.True(_control.SetFpsLimit(45));
        Assert.True(_control.SetFpsLimit(50));
        Assert.True(_control.SetFpsLimit(60));
        Assert.True(_control.SetFpsLimit(75));
        Assert.True(_control.SetFpsLimit(90));
        Assert.True(_control.SetFpsLimit(120));
        Assert.True(_control.SetFpsLimit(240));
    }

    [Fact]
    public void AllControllerModes_AreValidated()
    {
        RegisterAllMainPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        Assert.True(_control.SetControllerMode(ControllerMode.Auto));
        Assert.True(_control.SetControllerMode(ControllerMode.Gamepad));
        Assert.True(_control.SetControllerMode(ControllerMode.WASD));
        Assert.True(_control.SetControllerMode(ControllerMode.Mouse));
    }

    #endregion
}