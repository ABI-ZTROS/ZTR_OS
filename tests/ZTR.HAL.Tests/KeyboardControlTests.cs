using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class KeyboardControlTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<KeyboardControl>> _mockLogger;
    private AsusAcpi _acpi;
    private KeyboardControl _control;
    private bool _disposed;

    public KeyboardControlTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockLogger = new Mock<ILogger<KeyboardControl>>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new KeyboardControl(_acpi, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockLogger.Reset();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new KeyboardControl(_acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    #region SetBrightness Tests

    [Fact]
    public void SetBrightness_Level0_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBrightness(0);

        Assert.True(result);
    }

    [Fact]
    public void SetBrightness_Level1_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBrightness(1);

        Assert.True(result);
    }

    [Fact]
    public void SetBrightness_Level2_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBrightness(2);

        Assert.True(result);
    }

    [Fact]
    public void SetBrightness_Level3_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBrightness(3);

        Assert.True(result);
    }

    [Fact]
    public void SetBrightness_NegativeLevel_ReturnsFalse()
    {
        bool result = _control.SetBrightness(-1);

        Assert.False(result);
    }

    [Fact]
    public void SetBrightness_Above3_ReturnsFalse()
    {
        bool result = _control.SetBrightness(4);

        Assert.False(result);
    }

    [Fact]
    public void SetBrightness_Level10_ReturnsFalse()
    {
        bool result = _control.SetBrightness(10);

        Assert.False(result);
    }

    [Fact]
    public void SetBrightness_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetBrightness(2);

        Assert.False(result);
    }

    [Fact]
    public void SetBrightness_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(Array.Empty<byte>());

        bool result = _control.SetBrightness(2);

        Assert.False(result);
    }

    [Fact]
    public void SetBrightness_LogsWarning_ForInvalidLevel()
    {
        _control.SetBrightness(5);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetBrightness_LogsInformation_WhenSuccessful()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetBrightness(2);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetBrightness_LogsWarning_WhenAcpiFailsAfterException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Keyboard error"));

        bool result = _control.SetBrightness(2);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetBrightness Tests

    [Fact]
    public void GetBrightness_ReturnsLevelFromAcpi()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        int result = _control.GetBrightness();

        Assert.Equal(2, result);
    }

    [Fact]
    public void GetBrightness_ReturnsLevel0()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        int result = _control.GetBrightness();

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetBrightness_ReturnsLevel3()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(3));

        int result = _control.GetBrightness();

        Assert.Equal(3, result);
    }

    [Fact]
    public void GetBrightness_WhenDeviceUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetBrightness();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetBrightness_WhenAcpiFails_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _control.GetBrightness();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetBacklightZone Tests

    [Fact]
    public void SetBacklightZone_Zone1_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBacklightZone(KeyboardZone.Zone1);

        Assert.True(result);
    }

    [Fact]
    public void SetBacklightZone_Zone2_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBacklightZone(KeyboardZone.Zone2);

        Assert.True(result);
    }

    [Fact]
    public void SetBacklightZone_Zone3_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBacklightZone(KeyboardZone.Zone3);

        Assert.True(result);
    }

    [Fact]
    public void SetBacklightZone_Zone4_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetBacklightZone(KeyboardZone.Zone4);

        Assert.True(result);
    }

    [Fact]
    public void SetBacklightZone_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetBacklightZone(KeyboardZone.Zone1);

        Assert.False(result);
    }

    [Fact]
    public void SetBacklightZone_LogsInformation()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        _control.SetBacklightZone(KeyboardZone.Zone2);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetBacklightZone Tests

    [Fact]
    public void GetBacklightZone_ReturnsZone1()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.Equal(KeyboardZone.Zone1, _control.GetBacklightZone());
    }

    [Fact]
    public void GetBacklightZone_ReturnsZone4()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(3));

        Assert.Equal(KeyboardZone.Zone4, _control.GetBacklightZone());
    }

    [Fact]
    public void GetBacklightZone_WhenDeviceUnavailable_ReturnsZone1()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.Equal(KeyboardZone.Zone1, _control.GetBacklightZone());
    }

    [Fact]
    public void GetBacklightZone_WhenInvalidValue_ReturnsZone1()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(99));

        Assert.Equal(KeyboardZone.Zone1, _control.GetBacklightZone());
    }

    #endregion

    #region TurnOffBacklight Tests

    [Fact]
    public void TurnOffBacklight_CallsSetBrightnessWith0()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.TurnOffBacklight();

        Assert.True(result);
    }

    [Fact]
    public void TurnOffBacklight_WhenUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.TurnOffBacklight();

        Assert.False(result);
    }

    #endregion

    #region SetMaxBrightness Tests

    [Fact]
    public void SetMaxBrightness_CallsSetBrightnessWith3()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        bool result = _control.SetMaxBrightness();

        Assert.True(result);
    }

    [Fact]
    public void SetMaxBrightness_WhenUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetMaxBrightness();

        Assert.False(result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAcpi_DoesNotThrow()
    {
        var logger = new Mock<ILogger<KeyboardControl>>();
        Assert.NotNull(new KeyboardControl(_acpi, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullAcpi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyboardControl(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new KeyboardControl(_acpi));
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
    public void SetAndGetBrightness_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        bool setResult = _control.SetBrightness(2);
        int getResult = _control.GetBrightness();

        Assert.True(setResult);
        Assert.Equal(2, getResult);
    }

    [Fact]
    public void SetAndGetBacklightZone_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)KeyboardZone.Zone3));

        bool setResult = _control.SetBacklightZone(KeyboardZone.Zone3);
        var getResult = _control.GetBacklightZone();

        Assert.True(setResult);
        Assert.Equal(KeyboardZone.Zone3, getResult);
    }

    [Fact]
    public void AllBrightnessLevels_AreValidated()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        Assert.True(_control.SetBrightness(0));
        Assert.True(_control.SetBrightness(1));
        Assert.True(_control.SetBrightness(2));
        Assert.True(_control.SetBrightness(3));
    }

    [Fact]
    public void AllKeyboardZones_AreValidated()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        Assert.True(_control.SetBacklightZone(KeyboardZone.Zone1));
        Assert.True(_control.SetBacklightZone(KeyboardZone.Zone2));
        Assert.True(_control.SetBacklightZone(KeyboardZone.Zone3));
        Assert.True(_control.SetBacklightZone(KeyboardZone.Zone4));
    }

    #endregion
}