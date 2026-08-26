using Microsoft.Extensions.Logging;
using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class DeviceProbeTests
{
    private Mock<WmiHelper> _mockWmi;
    private Mock<IAtkDevice> _mockAtkDevice;
    private Mock<ILogger<AsusAcpi>> _mockAcpiLogger;
    private Mock<ILogger<DeviceProbe>> _mockProbeLogger;
    private AsusAcpi _acpi;
    private DeviceProbe _probe;

    public DeviceProbeTests()
    {
        _mockWmi = new Mock<WmiHelper>();
        _mockAtkDevice = new Mock<IAtkDevice>();
        _mockAcpiLogger = new Mock<ILogger<AsusAcpi>>();
        _mockProbeLogger = new Mock<ILogger<DeviceProbe>>();
        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockAtkDevice.Object, _mockAcpiLogger.Object);
        _probe = new DeviceProbe(_mockWmi.Object, _acpi, _mockProbeLogger.Object);
    }

    private void ResetMocks()
    {
        _mockWmi.Reset();
        _mockAtkDevice.Reset();
        _mockAcpiLogger.Reset();
        _mockProbeLogger.Reset();
        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _acpi = new AsusAcpi(_mockAtkDevice.Object, _mockAcpiLogger.Object);
        _probe = new DeviceProbe(_mockWmi.Object, _acpi, _mockProbeLogger.Object);
    }

    [Fact]
    public void Probe_PopulatesWmiInfo()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("ASUS ROG Strix G16");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("GL503PI.315");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("AMD Ryzen 9 7945HX");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("NVIDIA GeForce RTX 4090");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("ASUSTeK COMPUTER INC.");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        SetupAcpiSuccess();

        DeviceInfo result = _probe.Probe();

        Assert.Equal("ASUS ROG Strix G16", result.Model);
        Assert.Equal("GL503PI.315", result.BiosVersion);
        Assert.Equal("AMD Ryzen 9 7945HX", result.CpuModel);
        Assert.Equal("NVIDIA GeForce RTX 4090", result.GpuModel);
        Assert.Equal("ASUSTeK COMPUTER INC.", result.Manufacturer);
        Assert.Equal(DeviceType.Laptop, result.Type);
    }

    [Fact]
    public void Probe_DetectsPerformanceModeSupport()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer((int)AsusMode.PerformanceTurbo));

        DeviceInfo result = _probe.Probe();

        Assert.True(result.SupportsPerformanceModes);
        Assert.Contains("PerformanceModes", result.SupportedFeatures);
    }

    [Fact]
    public void Probe_DetectsFanControlSupport()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer(65));

        DeviceInfo result = _probe.Probe();

        Assert.True(result.SupportsFanControl);
        Assert.Contains("FanControl", result.SupportedFeatures);
    }

    [Fact]
    public void Probe_DetectsGpuModeSupport()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer((int)AsusGPU.Ultimate));

        DeviceInfo result = _probe.Probe();

        Assert.True(result.SupportsGpuModes);
        Assert.Contains("GPUModes", result.SupportedFeatures);
    }

    [Fact]
    public void Probe_DetectsBatteryLimitSupport()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer(80));

        DeviceInfo result = _probe.Probe();

        Assert.True(result.SupportsBatteryLimit);
        Assert.Contains("BatteryLimit", result.SupportedFeatures);
    }

    [Fact]
    public void Probe_DetectsAuraSupport()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer(2));

        DeviceInfo result = _probe.Probe();

        Assert.Contains("Aura", result.SupportedFeatures);
    }

    [Fact]
    public void Probe_WhenAtkAcpiUnavailable_SkipsAcpiDetection()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test Model");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("Test CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("Test GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Test Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(false);

        DeviceInfo result = _probe.Probe();

        Assert.False(result.IsAtkAcpiAvailable);
        Assert.Empty(result.SupportedFeatures);
        Assert.False(result.SupportsPerformanceModes);
        Assert.False(result.SupportsFanControl);
        Assert.False(result.SupportsGpuModes);
        Assert.False(result.SupportsBatteryLimit);
    }

    [Fact]
    public void Probe_WhenAllQueriesSucceed_ReturnsAllFeatures()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("ASUS ROG Strix G16");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("GL503PI.315");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("AMD Ryzen 9 7945HX");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("NVIDIA GeForce RTX 4090");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("ASUSTeK COMPUTER INC.");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer(42));

        DeviceInfo result = _probe.Probe();

        Assert.True(result.IsAtkAcpiAvailable);
        Assert.NotEmpty(result.SupportedFeatures);
        Assert.True(result.SupportsPerformanceModes);
        Assert.True(result.SupportsFanControl);
        Assert.True(result.SupportsGpuModes);
        Assert.True(result.SupportsBatteryLimit);
    }

    [Fact]
    public void Probe_LogsInformation_OnCompletion()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Returns("Test");
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        SetupAcpiSuccess();

        _probe.Probe();

        _mockProbeLogger.Verify(
            l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Probe_WmiException_DoesNotThrow()
    {
        _mockWmi.Setup(w => w.GetHardwareModel()).Throws<Exception>();
        _mockWmi.Setup(w => w.GetBiosVersion()).Returns("1.0");
        _mockWmi.Setup(w => w.GetCpuInfo()).Returns("CPU");
        _mockWmi.Setup(w => w.GetGpuInfo()).Returns("GPU");
        _mockWmi.Setup(w => w.GetManufacturer()).Returns("Mfg");
        _mockWmi.Setup(w => w.GetDeviceType()).Returns(DeviceType.Laptop);

        _mockAtkDevice.Setup(d => d.IsAvailable).Returns(true);
        SetupAcpiSuccess();

        DeviceInfo result = _probe.Probe();

        Assert.Equal(string.Empty, result.Model);
    }

    private void SetupAcpiSuccess()
    {
        _mockAtkDevice.Setup(d => d.CallControlBuffer(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Returns(BuildIntBuffer(42));
    }

    private static byte[] BuildIntBuffer(int value)
    {
        return BitConverter.GetBytes(value);
    }
}