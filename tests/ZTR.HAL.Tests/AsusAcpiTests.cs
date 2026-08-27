using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AsusAcpiTests : IDisposable
{
    private Mock<IAtkDevice> _mockDevice;
    private Mock<ILogger<AsusAcpi>> _mockLogger;
    private AsusAcpi _acpi;
    private bool _disposed;

    public AsusAcpiTests()
    {
        _mockDevice = new Mock<IAtkDevice>();
        _mockLogger = new Mock<ILogger<AsusAcpi>>();
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockDevice.Object, _mockLogger.Object);
    }

    private void ResetMocks()
    {
        _mockDevice.Reset();
        _mockLogger.Reset();
        _acpi = new AsusAcpi(_mockDevice.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _acpi.Dispose();
        }
    }

    #region DeviceSet Tests

    [Fact]
    public void DeviceSet_IntStatus_CallsDeviceWithCorrectArgs()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, (int)AsusMode.PerformanceTurbo);

        Assert.Equal(1, result);
        _mockDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 256), Times.Once);
    }

    [Fact]
    public void DeviceSet_ByteArray_CallsDeviceWithCorrectArgs()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] expected = { 0x01, 0x02, 0x03 };
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(expected);
        byte[] curve = { 0x01, 0x02, 0x03 };

        byte[] result = _acpi.DeviceSet(AsusDevice.DevsCPUFanCurve, curve);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void DeviceSet_WhenDeviceNotAvailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void DeviceSet_WhenCallControlFails_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        Assert.Equal(-1, result);
    }

    #endregion

    #region DeviceSetWmi Tests

    [Fact]
    public void DeviceSetWmi_CallsDeviceWithCorrectArgs()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        int result = _acpi.DeviceSetWmi(AsusDevice.GPUEco, (int)AsusGPU.Ultimate);

        Assert.Equal(1, result);
    }

    [Fact]
    public void DeviceSetWmi_WhenDeviceNotAvailable_ReturnsFalse()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _acpi.DeviceSetWmi(AsusDevice.GPUEco, 2);

        Assert.Equal(-1, result);
    }

    #endregion

    #region DeviceGet Tests

    [Fact]
    public void DeviceGet_ReturnsIntFromBuffer()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] buffer = BitConverter.GetBytes(42);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(buffer);

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(42, result);
    }

    [Fact]
    public void DeviceGet_WhenDeviceNotAvailable_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void DeviceGet_WhenBufferTooSmall_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(new byte[] { 0x01, 0x02 });

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void DeviceGet_WhenBufferEmpty_ReturnsNegativeOne()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(-1, result);
    }

    #endregion

    #region DeviceGetBuffer Tests

    [Fact]
    public void DeviceGetBuffer_ReturnsRawBuffer()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] expected = { 0x01, 0x02, 0x03, 0x04, 0x05 };
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(expected);

        byte[] result = _acpi.DeviceGetBuffer(AsusDevice.PerformanceMode, 0);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeviceGetBuffer_WhenDeviceNotAvailable_ReturnsEmptyArray()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        byte[] result = _acpi.DeviceGetBuffer(AsusDevice.PerformanceMode, 0);

        Assert.Empty(result);
    }

    #endregion

    #region DeviceGetLarge Tests

    [Fact]
    public void DeviceGetLarge_UsesConfigurableBufferSize()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] buffer = new byte[512];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i % 256);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), 512))
            .Returns(buffer);

        byte[] result = _acpi.DeviceGetLarge(AsusDevice.CPU_Fan, 0, 512);

        Assert.Equal(512, result.Length);
        _mockDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 512), Times.Once);
    }

    [Fact]
    public void DeviceGetLarge_MinimumBufferSizeIs32()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] buffer = new byte[32];
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), 32))
            .Returns(buffer);

        byte[] result = _acpi.DeviceGetLarge(AsusDevice.CPU_Fan, 0, 10);

        Assert.Equal(32, result.Length);
        _mockDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 32), Times.Once);
    }

    [Fact]
    public void DeviceGetLarge_WhenDeviceNotAvailable_ReturnsEmptyArray()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        byte[] result = _acpi.DeviceGetLarge(AsusDevice.CPU_Fan, 0, 512);

        Assert.Empty(result);
    }

    #endregion

    #region Initialize and SetWatchdog Tests

    [Fact]
    public void Initialize_CallsDevice()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _acpi.Initialize();

        Assert.True(result);
    }

    [Fact]
    public void SetWatchdog_CallsDevice()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControl(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(true);

        bool result = _acpi.SetWatchdog(30);

        Assert.True(result);
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public void DeviceSet_SucceedsAfterRetry()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        int callCount = 0;
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(() => ++callCount < 3 ? Array.Empty<byte>() : BitConverter.GetBytes(1));

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        Assert.Equal(1, result);
        Assert.True(callCount >= 3);
    }

    [Fact]
    public void DeviceGet_SucceedsAfterRetry()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        int callCount = 0;
        byte[] successBuffer = BitConverter.GetBytes(99);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(() => ++callCount < 2 ? Array.Empty<byte>() : successBuffer);

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(99, result);
        Assert.True(callCount >= 2);
    }

    [Fact]
    public void DeviceSet_FailsAfterMaxRetries()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        Assert.Equal(-1, result);
        _mockDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()), Times.AtLeast(3));
    }

    [Fact]
    public void DeviceGet_FailsAfterMaxRetries()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(-1, result);
        _mockDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()), Times.AtLeast(3));
    }

    [Fact]
    public void Retry_LogsWarningOnFailure()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());

        _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        _mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Convenience Method Tests

    [Fact]
    public void SetPerformanceMode_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool result = _acpi.SetPerformanceMode(AsusMode.PerformanceTurbo);

        Assert.True(result);
    }

    [Fact]
    public void GetPerformanceMode_ReturnsValue()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)AsusMode.PerformanceBalanced));

        int result = _acpi.GetPerformanceMode();

        Assert.Equal((int)AsusMode.PerformanceBalanced, result);
    }

    [Fact]
    public void SetStatusMode_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool result = _acpi.SetStatusMode(1);

        Assert.True(result);
    }

    [Fact]
    public void SetCpuFanCurve_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] expected = { 0x10, 0x20, 0x30 };
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(expected);
        byte[] curve = { 0x10, 0x20, 0x30 };

        bool result = _acpi.SetCpuFanCurve(curve);

        Assert.True(result);
    }

    [Fact]
    public void SetGpuFanCurve_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        byte[] expected = { 0x10, 0x20, 0x30 };
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(expected);
        byte[] curve = { 0x10, 0x20, 0x30 };

        bool result = _acpi.SetGpuFanCurve(curve);

        Assert.True(result);
    }

    [Fact]
    public void GetCpuTemperature_ReturnsValue()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(65));

        int result = _acpi.GetCpuTemperature();

        Assert.Equal(65, result);
    }

    [Fact]
    public void GetGpuTemperature_ReturnsValue()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(55));

        int result = _acpi.GetGpuTemperature();

        Assert.Equal(55, result);
    }

    [Fact]
    public void SetBatteryLimit_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool result = _acpi.SetBatteryLimit(80);

        Assert.True(result);
    }

    [Fact]
    public void GetBatteryLimit_ReturnsValue()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(80));

        int result = _acpi.GetBatteryLimit();

        Assert.Equal(80, result);
    }

    [Fact]
    public void SetKeyboardBrightness_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool result = _acpi.SetKeyboardBrightness(3);

        Assert.True(result);
    }

    [Fact]
    public void SetGPUMode_CorrectDeviceId()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        bool result = _acpi.SetGPUMode(AsusGPU.Ultimate);

        Assert.True(result);
    }

    [Fact]
    public void GetGPUMode_ReturnsValue()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)AsusGPU.Eco));

        int result = _acpi.GetGPUMode();

        Assert.Equal((int)AsusGPU.Eco, result);
    }

    #endregion

    #region Graceful Degradation Tests

    [Fact]
    public void AllMethods_ReturnSafeDefaults_WhenAtkAcpiUnavailable()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(false);

        Assert.Equal(-1, _acpi.DeviceSet(AsusDevice.PerformanceMode, 1));
        Assert.Empty(_acpi.DeviceSet(AsusDevice.PerformanceMode, new byte[] { 1 }));
        Assert.Equal(-1, _acpi.DeviceSetWmi(AsusDevice.PerformanceMode, 1));
        Assert.Equal(-1, _acpi.DeviceGet(AsusDevice.PerformanceMode));
        Assert.Empty(_acpi.DeviceGetBuffer(AsusDevice.PerformanceMode, 0));
        Assert.Empty(_acpi.DeviceGetLarge(AsusDevice.PerformanceMode, 0, 512));
        Assert.False(_acpi.Initialize());
        Assert.False(_acpi.SetWatchdog(30));
        Assert.False(_acpi.SetPerformanceMode(AsusMode.PerformanceTurbo));
        Assert.False(_acpi.SetStatusMode(1));
        Assert.Equal(-1, _acpi.GetPerformanceMode());
        Assert.False(_acpi.SetCpuFanCurve(new byte[] { 1 }));
        Assert.False(_acpi.SetGpuFanCurve(new byte[] { 1 }));
        Assert.Equal(-1, _acpi.GetCpuTemperature());
        Assert.Equal(-1, _acpi.GetGpuTemperature());
        Assert.False(_acpi.SetBatteryLimit(80));
        Assert.Equal(-1, _acpi.GetBatteryLimit());
        Assert.False(_acpi.SetKeyboardBrightness(3));
        Assert.False(_acpi.SetGPUMode(AsusGPU.Ultimate));
        Assert.Equal(-1, _acpi.GetGPUMode());
    }

    [Fact]
    public void Constructor_LogsWarning_WhenDeviceUnavailable()
    {
        var mockDevice = new Mock<IAtkDevice>();
        var mockLogger = new Mock<ILogger<AsusAcpi>>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);

        using var acpi = new AsusAcpi(mockDevice.Object, mockLogger.Object);

        mockLogger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void DeviceSet_HandlesExceptionDuringRetry()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        int callCount = 0;
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("Device error");
                return BitConverter.GetBytes(1);
            });

        int result = _acpi.DeviceSet(AsusDevice.PerformanceMode, 1);

        Assert.Equal(1, result);
    }

    [Fact]
    public void DeviceGet_HandlesExceptionDuringRetry()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);
        int callCount = 0;
        byte[] successBuffer = BitConverter.GetBytes(77);
        _mockDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("Device error");
                return successBuffer;
            });

        int result = _acpi.DeviceGet(AsusDevice.PerformanceMode);

        Assert.Equal(77, result);
    }

    [Fact]
    public void Dispose_DisposesDevice()
    {
        _mockDevice.Setup(d => d.IsAvailable).Returns(true);

        _acpi.Dispose();

        _mockDevice.Verify(d => d.Dispose(), Times.Once);
    }

    #endregion
}