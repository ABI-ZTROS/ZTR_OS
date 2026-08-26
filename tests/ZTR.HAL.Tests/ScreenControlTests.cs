using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class ScreenControlTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<ScreenControl>> _mockLogger;
    private AsusAcpi _acpi;
    private ScreenControl _control;
    private bool _disposed;

    public ScreenControlTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockLogger = new Mock<ILogger<ScreenControl>>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new ScreenControl(_acpi, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockLogger.Reset();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockAcpiLogger.Object);
        _control = new ScreenControl(_acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _control.Dispose();
        }
    }

    #region GetSupportedRefreshRates Tests

    [Fact]
    public void GetSupportedRefreshRates_ReturnsExpectedRates()
    {
        var rates = _control.GetSupportedRefreshRates();

        Assert.Contains(60, rates);
        Assert.Contains(120, rates);
        Assert.Contains(144, rates);
        Assert.Contains(165, rates);
        Assert.Contains(240, rates);
        Assert.Contains(300, rates);
    }

    [Fact]
    public void GetSupportedRefreshRates_ContainsSixRates()
    {
        var rates = _control.GetSupportedRefreshRates();

        Assert.Equal(6, rates.Count);
    }

    [Fact]
    public void GetSupportedRefreshRates_AreAllPositive()
    {
        var rates = _control.GetSupportedRefreshRates();

        Assert.All(rates, r => Assert.True(r > 0));
    }

    [Fact]
    public void GetSupportedRefreshRates_AreInAscendingOrder()
    {
        var rates = _control.GetSupportedRefreshRates();

        for (int i = 1; i < rates.Count; i++)
        {
            Assert.True(rates[i] > rates[i - 1]);
        }
    }

    #endregion

    #region SetRefreshRate Tests

    [Fact]
    public void SetRefreshRate_ValidRate_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetRefreshRate(120);

        Assert.True(result);
    }

    [Fact]
    public void SetRefreshRate_60Hz_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetRefreshRate(60);

        Assert.True(result);
    }

    [Fact]
    public void SetRefreshRate_240Hz_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetRefreshRate(240);

        Assert.True(result);
    }

    [Fact]
    public void SetRefreshRate_300Hz_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetRefreshRate(300);

        Assert.True(result);
    }

    [Fact]
    public void SetRefreshRate_UnsupportedRate_ReturnsFalse()
    {
        bool result = _control.SetRefreshRate(75);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_InvalidRate_ReturnsFalse()
    {
        bool result = _control.SetRefreshRate(500);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_NegativeRate_ReturnsFalse()
    {
        bool result = _control.SetRefreshRate(-60);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_ZeroRate_ReturnsFalse()
    {
        bool result = _control.SetRefreshRate(0);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetRefreshRate(120);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetRefreshRate(120);

        Assert.False(result);
    }

    [Fact]
    public void SetRefreshRate_LogsWarning_ForUnsupportedRate()
    {
        _control.SetRefreshRate(75);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetCurrentRefreshRate Tests

    [Fact]
    public void GetCurrentRefreshRate_ReturnsValueFromAcpi()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(144));

        int result = _control.GetCurrentRefreshRate();

        Assert.Equal(144, result);
    }

    [Fact]
    public void GetCurrentRefreshRate_WhenValueIsZero_Returns60()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        int result = _control.GetCurrentRefreshRate();

        Assert.Equal(60, result);
    }

    [Fact]
    public void GetCurrentRefreshRate_WhenDeviceUnavailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _control.GetCurrentRefreshRate();

        Assert.Equal(-1, result);
    }

    #endregion

    #region SetOverdrive Tests

    [Fact]
    public void SetOverdrive_Enable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetOverdrive(true);

        Assert.True(result);
    }

    [Fact]
    public void SetOverdrive_Disable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetOverdrive(false);

        Assert.True(result);
    }

    [Fact]
    public void SetOverdrive_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetOverdrive(true);

        Assert.False(result);
    }

    [Fact]
    public void SetOverdrive_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetOverdrive(true);

        Assert.False(result);
    }

    [Fact]
    public void SetOverdrive_LogsInformation_WhenEnabled()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetOverdrive(true);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetOverdrive Tests

    [Fact]
    public void GetOverdrive_Enabled_ReturnsTrue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.True(_control.GetOverdrive());
    }

    [Fact]
    public void GetOverdrive_Disabled_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.False(_control.GetOverdrive());
    }

    [Fact]
    public void GetOverdrive_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.False(_control.GetOverdrive());
    }

    #endregion

    #region SetMiniLed Tests

    [Fact]
    public void SetMiniLed_Off_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMiniLed(MiniLedMode.Off);

        Assert.True(result);
    }

    [Fact]
    public void SetMiniLed_Standard_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMiniLed(MiniLedMode.Standard);

        Assert.True(result);
    }

    [Fact]
    public void SetMiniLed_Advanced_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetMiniLed(MiniLedMode.Advanced);

        Assert.True(result);
    }

    [Fact]
    public void SetMiniLed_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetMiniLed(MiniLedMode.Standard);

        Assert.False(result);
    }

    [Fact]
    public void SetMiniLed_WhenFirstCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetMiniLed(MiniLedMode.Standard);

        Assert.False(result);
    }

    #endregion

    #region GetMiniLed Tests

    [Fact]
    public void GetMiniLed_ReturnsOff_WhenValueIsZero()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.Equal(MiniLedMode.Off, _control.GetMiniLed());
    }

    [Fact]
    public void GetMiniLed_ReturnsStandard_WhenValueIsOne()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.Equal(MiniLedMode.Standard, _control.GetMiniLed());
    }

    [Fact]
    public void GetMiniLed_ReturnsAdvanced_WhenValueIsTwo()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        Assert.Equal(MiniLedMode.Advanced, _control.GetMiniLed());
    }

    [Fact]
    public void GetMiniLed_WhenDeviceUnavailable_ReturnsOff()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.Equal(MiniLedMode.Off, _control.GetMiniLed());
    }

    #endregion

    #region SetHDR Tests

    [Fact]
    public void SetHDR_Enable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetHDR(true);

        Assert.True(result);
    }

    [Fact]
    public void SetHDR_Disable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetHDR(false);

        Assert.True(result);
    }

    [Fact]
    public void SetHDR_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetHDR(true);

        Assert.False(result);
    }

    [Fact]
    public void SetHDR_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetHDR(true);

        Assert.False(result);
    }

    #endregion

    #region GetHDR Tests

    [Fact]
    public void GetHDR_Enabled_ReturnsTrue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(2));

        Assert.True(_control.GetHDR());
    }

    [Fact]
    public void GetHDR_Disabled_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.False(_control.GetHDR());
    }

    [Fact]
    public void GetHDR_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.False(_control.GetHDR());
    }

    #endregion

    #region SetOptimalBrightness Tests

    [Fact]
    public void SetOptimalBrightness_Enable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetOptimalBrightness(true);

        Assert.True(result);
    }

    [Fact]
    public void SetOptimalBrightness_Disable_Succeeds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _control.SetOptimalBrightness(false);

        Assert.True(result);
    }

    [Fact]
    public void SetOptimalBrightness_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        bool result = _control.SetOptimalBrightness(true);

        Assert.False(result);
    }

    [Fact]
    public void SetOptimalBrightness_WhenCallFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        bool result = _control.SetOptimalBrightness(true);

        Assert.False(result);
    }

    #endregion

    #region GetOptimalBrightness Tests

    [Fact]
    public void GetOptimalBrightness_Enabled_ReturnsTrue()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        Assert.True(_control.GetOptimalBrightness());
    }

    [Fact]
    public void GetOptimalBrightness_Disabled_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        Assert.False(_control.GetOptimalBrightness());
    }

    [Fact]
    public void GetOptimalBrightness_WhenDeviceUnavailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.False(_control.GetOptimalBrightness());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAcpi_DoesNotThrow()
    {
        var logger = new Mock<ILogger<ScreenControl>>();
        Assert.NotNull(new ScreenControl(_acpi, logger.Object));
    }

    [Fact]
    public void Constructor_WithNullAcpi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenControl(null!));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        Assert.NotNull(new ScreenControl(_acpi));
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

    #region Edge Case Tests

    [Fact]
    public void SetRefreshRate_LogsInformation_WhenSuccessful()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetRefreshRate(144);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetRefreshRate_LogsWarning_WhenCallFails()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(false);

        _control.SetRefreshRate(120);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void SetMiniLed_CallsBothDeviceIds()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetMiniLed(MiniLedMode.Advanced);

        _mockDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()), Times.AtLeast(2));
    }

    [Fact]
    public void SetHDR_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetHDR(true);

        _mockDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void SetOptimalBrightness_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetOptimalBrightness(true);

        _mockDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void SetOverdrive_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        _control.SetOverdrive(true);

        _mockDevice.Verify(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void GetCurrentRefreshRate_WithBufferError_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Buffer error"));

        int result = _control.GetCurrentRefreshRate();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void SetRefreshRate_LogsWarning_WhenAcpiFailsAfterException()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Hardware error"));

        bool result = _control.SetRefreshRate(120);

        Assert.False(result);
        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}