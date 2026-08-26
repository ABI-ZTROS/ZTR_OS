using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class XgmControlTests : IDisposable
{
    private Mock<ILogger<XgmControl>> _mockLogger;
    private Mock<IHidReportWriter> _mockWriter;
    private AsusHid _hid;
    private XgmControl _control;
    private bool _disposed;

    public XgmControlTests()
    {
        _mockLogger = new Mock<ILogger<XgmControl>>();
        _mockWriter = new Mock<IHidReportWriter>();
        _hid = new AsusHid(_mockWriter.Object);
        _control = new XgmControl(_hid, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockLogger.Reset();
        _mockWriter.Reset();
        _hid = new AsusHid(_mockWriter.Object);
        _control = new XgmControl(_hid, _mockLogger.Object);
    }

    private void RegisterAllXgmPids()
    {
        foreach (var pid in AsusHid.XgmPids)
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

    #region Initialize Tests

    [Fact]
    public void Initialize_WhenDevicesAvailable_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.Initialize();

        Assert.True(result);
        Assert.True(_control.IsInitialized);
    }

    [Fact]
    public void Initialize_WhenNoDevices_Fails()
    {
        bool result = _control.Initialize();

        Assert.False(result);
        Assert.False(_control.IsInitialized);
    }

    [Fact]
    public void Initialize_LogsInformation_WhenSuccessful()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.Initialize();

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Initialize_LogsWarning_WhenFails()
    {
        _control.Initialize();

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Initialize_LogsWarning_WhenHidFailsAfterException()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("XGM error"));

        bool result = _control.Initialize();

        Assert.False(result);
        Assert.False(_control.IsInitialized);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SetLighting Tests

    [Fact]
    public void SetLighting_Enable_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetLighting(true);

        Assert.True(result);
        Assert.True(_control.IsLightingEnabled);
    }

    [Fact]
    public void SetLighting_Disable_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetLighting(false);

        Assert.True(result);
        Assert.False(_control.IsLightingEnabled);
    }

    [Fact]
    public void SetLighting_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.SetLighting(true);

        Assert.False(result);
    }

    [Fact]
    public void SetLighting_LogsInformation()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetLighting(true);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetLighting_LogsWarning_WhenHidFailsAfterException()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("Lighting error"));

        bool result = _control.SetLighting(true);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SetFanSpeed Tests

    [Fact]
    public void SetFanSpeed_ValidSpeed_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetFanSpeed(50);

        Assert.True(result);
        Assert.Equal(50, _control.CurrentFanSpeed);
    }

    [Fact]
    public void SetFanSpeed_Zero_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetFanSpeed(0);

        Assert.True(result);
        Assert.Equal(0, _control.CurrentFanSpeed);
    }

    [Fact]
    public void SetFanSpeed_100_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetFanSpeed(100);

        Assert.True(result);
        Assert.Equal(100, _control.CurrentFanSpeed);
    }

    [Fact]
    public void SetFanSpeed_Negative_ReturnsFalse()
    {
        bool result = _control.SetFanSpeed(-1);

        Assert.False(result);
    }

    [Fact]
    public void SetFanSpeed_Above100_ReturnsFalse()
    {
        bool result = _control.SetFanSpeed(101);

        Assert.False(result);
    }

    [Fact]
    public void SetFanSpeed_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.SetFanSpeed(50);

        Assert.False(result);
    }

    [Fact]
    public void SetFanSpeed_LogsWarning_ForInvalid()
    {
        _control.SetFanSpeed(101);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetFanSpeed_LogsInformation()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetFanSpeed(75);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SetAutoFanSpeed Tests

    [Fact]
    public void SetAutoFanSpeed_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetAutoFanSpeed();

        Assert.True(result);
    }

    [Fact]
    public void SetAutoFanSpeed_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.SetAutoFanSpeed();

        Assert.False(result);
    }

    #endregion

    #region SetLightingColor Tests

    [Fact]
    public void SetLightingColor_ValidColor_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetLightingColor(255, 0, 0);

        Assert.True(result);
    }

    [Fact]
    public void SetLightingColor_Black_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetLightingColor(0, 0, 0);

        Assert.True(result);
    }

    [Fact]
    public void SetLightingColor_White_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.SetLightingColor(255, 255, 255);

        Assert.True(result);
    }

    [Fact]
    public void SetLightingColor_WhenNoDevices_ReturnsFalse()
    {
        bool result = _control.SetLightingColor(255, 0, 0);

        Assert.False(result);
    }

    [Fact]
    public void SetLightingColor_LogsInformation()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetLightingColor(128, 64, 32);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region TurnOffLighting Tests

    [Fact]
    public void TurnOffLighting_CallsSetLightingFalse()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.TurnOffLighting();

        Assert.True(result);
        Assert.False(_control.IsLightingEnabled);
    }

    #endregion

    #region Property State Tests

    [Fact]
    public void IsInitialized_DefaultIsFalse()
    {
        Assert.False(_control.IsInitialized);
    }

    [Fact]
    public void CurrentFanSpeed_DefaultIsZero()
    {
        Assert.Equal(0, _control.CurrentFanSpeed);
    }

    [Fact]
    public void IsLightingEnabled_DefaultIsFalse()
    {
        Assert.False(_control.IsLightingEnabled);
    }

    [Fact]
    public void CurrentFanSpeed_UpdatesAfterSet()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetFanSpeed(80);

        Assert.Equal(80, _control.CurrentFanSpeed);
    }

    [Fact]
    public void IsLightingEnabled_UpdatesAfterSet()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        _control.SetLighting(true);

        Assert.True(_control.IsLightingEnabled);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidHid_DoesNotThrow()
    {
        var logger = new Mock<ILogger<XgmControl>>();
        Assert.NotNull(new XgmControl(_hid, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullHid_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new XgmControl(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new XgmControl(_hid));
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

    [Fact]
    public void Dispose_SetsIsInitializedToFalse()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();
        _control.Initialize();

        _control.Dispose();

        Assert.False(_control.IsInitialized);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void InitializeAndSetLighting_FullWorkflow()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool initResult = _control.Initialize();
        bool lightResult = _control.SetLighting(true);

        Assert.True(initResult);
        Assert.True(lightResult);
        Assert.True(_control.IsLightingEnabled);
    }

    [Fact]
    public void SetFanSpeed_AllValidLevels()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        Assert.True(_control.SetFanSpeed(0));
        Assert.True(_control.SetFanSpeed(25));
        Assert.True(_control.SetFanSpeed(50));
        Assert.True(_control.SetFanSpeed(75));
        Assert.True(_control.SetFanSpeed(100));
    }

    [Fact]
    public void Initialize_WithMultipleXgmPids_Succeeds()
    {
        RegisterAllXgmPids();
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        bool result = _control.Initialize();

        Assert.True(result);
    }

    #endregion
}