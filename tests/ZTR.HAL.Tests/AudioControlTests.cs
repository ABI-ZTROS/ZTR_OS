using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AudioControlTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<AudioControl>> _mockLogger;
    private AsusAcpi _acpi;
    private AudioControl _control;
    private bool _disposed;

    public AudioControlTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockLogger = new Mock<ILogger<AudioControl>>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new AudioControl(_acpi, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockLogger.Reset();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new AudioControl(_acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    #region SetMasterMute Tests

    [Fact]
    public void SetMasterMute_Mute_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMasterMute(true);

        Assert.True(result);
    }

    [Fact]
    public void SetMasterMute_Unmute_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMasterMute(false);

        Assert.True(result);
    }

    [Fact]
    public void SetMasterMute_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetMasterMute(true);

        Assert.False(result);
    }

    [Fact]
    public void SetMasterMute_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetMasterMute(true);

        Assert.False(result);
    }

    [Fact]
    public void SetMasterMute_LogsInformation_WhenMuted()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMasterMute(true);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetMasterMute_LogsInformation_WhenUnmuted()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMasterMute(false);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetMasterMute_LogsWarning_WhenCallFails()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        _control.SetMasterMute(true);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetMasterMute_LogsWarning_WhenAcpiFailsAfterException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Audio error"));

        bool result = _control.SetMasterMute(true);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetMasterMute_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMasterMute(true);

        _mockDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once);
    }

    #endregion

    #region GetMasterMute Tests

    [Fact]
    public void GetMasterMute_Muted_ReturnsTrue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.True(_control.GetMasterMute());
    }

    [Fact]
    public void GetMasterMute_Unmuted_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.False(_control.GetMasterMute());
    }

    [Fact]
    public void GetMasterMute_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.False(_control.GetMasterMute());
    }

    [Fact]
    public void GetMasterMute_WhenBufferError_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Read error"));

        Assert.False(_control.GetMasterMute());
    }

    #endregion

    #region SetMicMute Tests

    [Fact]
    public void SetMicMute_Mute_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMicMute(true);

        Assert.True(result);
    }

    [Fact]
    public void SetMicMute_Unmute_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMicMute(false);

        Assert.True(result);
    }

    [Fact]
    public void SetMicMute_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetMicMute(true);

        Assert.False(result);
    }

    [Fact]
    public void SetMicMute_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetMicMute(true);

        Assert.False(result);
    }

    [Fact]
    public void SetMicMute_LogsInformation()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMicMute(true);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetMicMute_LogsWarning_WhenAcpiFailsAfterException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Mic error"));

        bool result = _control.SetMicMute(true);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetMicMute Tests

    [Fact]
    public void GetMicMute_Muted_ReturnsTrue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.True(_control.GetMicMute());
    }

    [Fact]
    public void GetMicMute_Unmuted_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.False(_control.GetMicMute());
    }

    [Fact]
    public void GetMicMute_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.False(_control.GetMicMute());
    }

    #endregion

    #region GetMasterVolume Tests

    [Fact]
    public void GetMasterVolume_ReturnsValue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(75));

        int result = _control.GetMasterVolume();

        Assert.Equal(75, result);
    }

    [Fact]
    public void GetMasterVolume_WhenUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetMasterVolume();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetMasterVolume_WhenAcpiFails_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _control.GetMasterVolume();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetMasterVolume Tests

    [Fact]
    public void SetMasterVolume_ValidLevel_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMasterVolume(50);

        Assert.True(result);
    }

    [Fact]
    public void SetMasterVolume_Zero_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMasterVolume(0);

        Assert.True(result);
    }

    [Fact]
    public void SetMasterVolume_100_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMasterVolume(100);

        Assert.True(result);
    }

    [Fact]
    public void SetMasterVolume_Negative_ReturnsFalse()
    {
        bool result = _control.SetMasterVolume(-1);

        Assert.False(result);
    }

    [Fact]
    public void SetMasterVolume_Above100_ReturnsFalse()
    {
        bool result = _control.SetMasterVolume(101);

        Assert.False(result);
    }

    [Fact]
    public void SetMasterVolume_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetMasterVolume(50);

        Assert.False(result);
    }

    [Fact]
    public void SetMasterVolume_LogsWarning_ForInvalidLevel()
    {
        _control.SetMasterVolume(-1);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetMasterVolume_LogsInformation()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMasterVolume(50);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAcpi_DoesNotThrow()
    {
        var logger = new Mock<ILogger<AudioControl>>();
        Assert.NotNull(new AudioControl(_acpi, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullAcpi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AudioControl(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new AudioControl(_acpi));
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
    public void SetAndGetMasterMute_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool setResult = _control.SetMasterMute(true);
        bool getResult = _control.GetMasterMute();

        Assert.True(setResult);
        Assert.True(getResult);
    }

    [Fact]
    public void SetAndGetMicMute_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool setResult = _control.SetMicMute(true);
        bool getResult = _control.GetMicMute();

        Assert.True(setResult);
        Assert.True(getResult);
    }

    [Fact]
    public void SetAndGetMasterVolume_RoundTrip()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(75));

        bool setResult = _control.SetMasterVolume(75);
        int getResult = _control.GetMasterVolume();

        Assert.True(setResult);
        Assert.Equal(75, getResult);
    }

    #endregion
}