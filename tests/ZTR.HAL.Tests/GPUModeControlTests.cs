using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class GPUModeControlTests : IDisposable
{
    private readonly Mock<IGpuControl> _mockGpuControl;
    private readonly Mock<IAtkDevice> _mockAtkDevice;
    private readonly Mock<ILogger<GPUModeControl>> _mockLogger;
    private readonly AsusAcpi _acpi;
    private readonly GPUModeControl _modeControl;
    private bool _disposed;

    public GPUModeControlTests()
    {
        _mockGpuControl = new Mock<IGpuControl>();
        _mockAtkDevice = new Mock<IAtkDevice>();
        _mockLogger = new Mock<ILogger<GPUModeControl>>();

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockAtkDevice.Object);
        _modeControl = new GPUModeControl(_mockGpuControl.Object, _acpi, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _modeControl.Dispose();
            _acpi.Dispose();
        }
    }

    [Fact]
    public void SetGpuMode_Eco_CallsAcpiCorrectly()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        var result = _modeControl.SetGpuMode(AsusGPU.Eco);

        Assert.True(result);
        _mockAtkDevice.Verify(d => d.CallControlBuffer(It.IsAny<byte[]>(), 256), Times.AtLeastOnce);
    }

    [Fact]
    public void SetGpuMode_Standard_CallsAcpiCorrectly()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        var result = _modeControl.SetGpuMode(AsusGPU.Standard);

        Assert.True(result);
    }

    [Fact]
    public void SetGpuMode_Ultimate_CallsAcpiCorrectly()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        var result = _modeControl.SetGpuMode(AsusGPU.Ultimate);

        Assert.True(result);
    }

    [Fact]
    public void SetGpuMode_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(Array.Empty<byte>());

        var result = _modeControl.SetGpuMode(AsusGPU.Standard);

        Assert.False(result);
    }

    [Fact]
    public void SetGpuMode_WhenAcpiUnavailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var modeControl = new GPUModeControl(_mockGpuControl.Object, acpi);

        var result = modeControl.SetGpuMode(AsusGPU.Ultimate);

        Assert.False(result);
    }

    [Fact]
    public void GetGpuMode_ReturnsCurrentMode()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes((int)AsusGPU.Eco));

        var result = _modeControl.GetGpuMode();

        Assert.Equal(AsusGPU.Eco, result);
    }

    [Fact]
    public void GetGpuMode_WhenAcpiUnavailable_ReturnsStandard()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var modeControl = new GPUModeControl(_mockGpuControl.Object, acpi);

        var result = modeControl.GetGpuMode();

        Assert.Equal(AsusGPU.Standard, result);
    }

    [Fact]
    public void SetMux_Enable_CallsAcpi()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        var result = _modeControl.SetMux(true);

        Assert.True(result);
    }

    [Fact]
    public void SetMux_Disable_CallsAcpi()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));

        var result = _modeControl.SetMux(false);

        Assert.True(result);
    }

    [Fact]
    public void SetMux_WhenAcpiFails_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(Array.Empty<byte>());

        var result = _modeControl.SetMux(true);

        Assert.False(result);
    }

    [Fact]
    public void GetMux_WhenEnabled_ReturnsTrue()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(1));

        var result = _modeControl.GetMux();

        Assert.True(result);
    }

    [Fact]
    public void GetMux_WhenDisabled_ReturnsFalse()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(0));

        var result = _modeControl.GetMux();

        Assert.False(result);
    }

    [Fact]
    public void AutoDetectGpuApps_WhenNoGamesRunning_ReturnsEmptyList()
    {
        var result = _modeControl.AutoDetectGpuApps();

        Assert.Empty(result);
    }

    [Fact]
    public void AutoDetectGpuApps_WhenAcpiUnavailable_DoesNotThrow()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var modeControl = new GPUModeControl(_mockGpuControl.Object, acpi);

        var result = modeControl.AutoDetectGpuApps();

        Assert.NotNull(result);
    }

    [Fact]
    public void SetGpuMode_AppliesFanSpeedForUltimate()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockGpuControl.Setup(g => g.SetFanSpeed(100)).Returns(true);

        _modeControl.SetGpuMode(AsusGPU.Ultimate);

        _mockGpuControl.Verify(g => g.SetFanSpeed(100), Times.Once);
    }

    [Fact]
    public void SetGpuMode_AppliesFanSpeedForStandard()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockGpuControl.Setup(g => g.SetFanSpeed(50)).Returns(true);

        _modeControl.SetGpuMode(AsusGPU.Standard);

        _mockGpuControl.Verify(g => g.SetFanSpeed(50), Times.Once);
    }

    [Fact]
    public void SetGpuMode_AppliesFanSpeedForEco()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockGpuControl.Setup(g => g.SetFanSpeed(0)).Returns(true);

        _modeControl.SetGpuMode(AsusGPU.Eco);

        _mockGpuControl.Verify(g => g.SetFanSpeed(0), Times.Once);
    }

    [Fact]
    public void SetGpuMode_WhenGpuControlThrows_StillReturnsTrue()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(BitConverter.GetBytes(1));
        _mockGpuControl.Setup(g => g.SetFanSpeed(It.IsAny<int>())).Throws<InvalidOperationException>();

        var result = _modeControl.SetGpuMode(AsusGPU.Standard);

        Assert.True(result);
    }

    [Fact]
    public void GetGpuMode_WhenInvalidMode_ReturnsStandard()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BitConverter.GetBytes(999));

        var result = _modeControl.GetGpuMode();

        Assert.Equal(AsusGPU.Standard, result);
    }

    [Fact]
    public void SetMux_WhenDeviceNotAvailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var modeControl = new GPUModeControl(_mockGpuControl.Object, acpi);

        var result = modeControl.SetMux(true);

        Assert.False(result);
    }

    [Fact]
    public void GetMux_WhenDeviceNotAvailable_ReturnsFalse()
    {
        var mockDevice = new Mock<IAtkDevice>();
        mockDevice.Setup(d => d.IsAvailable).Returns(false);
        using var acpi = new AsusAcpi(mockDevice.Object);
        using var modeControl = new GPUModeControl(_mockGpuControl.Object, acpi);

        var result = modeControl.GetMux();

        Assert.False(result);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _modeControl.Dispose();
        _modeControl.Dispose();
    }
}