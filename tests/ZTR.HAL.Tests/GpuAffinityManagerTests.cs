using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class GpuAffinityManagerTests
{
    private readonly Mock<INvApiGpu> _mockNvApi;
    private readonly Mock<IAdl2Gpu> _mockAdl2;
    private readonly GpuAffinityManager _manager;

    public GpuAffinityManagerTests()
    {
        _mockNvApi = new Mock<INvApiGpu>();
        _mockAdl2 = new Mock<IAdl2Gpu>();
        _manager = new GpuAffinityManager(_mockNvApi.Object, _mockAdl2.Object);
    }

    [Fact]
    public void Constructor_WithNullDependencies_CreatesInstance()
    {
        var manager = new GpuAffinityManager(null, null);
        Assert.NotNull(manager);
        Assert.False(manager.IsGpuAvailable);
    }

    [Fact]
    public void IsGpuAvailable_ReturnsFalse_WhenBothSdksUnavailable()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(false);

        Assert.False(_manager.IsGpuAvailable);
    }

    [Fact]
    public void IsGpuAvailable_ReturnsTrue_WhenNvApiAvailable()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(false);

        Assert.True(_manager.IsGpuAvailable);
    }

    [Fact]
    public void IsGpuAvailable_ReturnsTrue_WhenAdl2Available()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(true);

        Assert.True(_manager.IsGpuAvailable);
    }

    [Fact]
    public void SetGpuAffinity_InvalidProcessId_ReturnsFalse()
    {
        Assert.False(_manager.SetGpuAffinity(0, 0));
        Assert.False(_manager.SetGpuAffinity(-1, 0));
    }

    [Fact]
    public void SetGpuAffinity_NegativeGpuIndex_ReturnsFalse()
    {
        Assert.False(_manager.SetGpuAffinity(1, -1));
    }

    [Fact]
    public void SetGpuAffinity_ValidInputs_UpdatesCache()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        int processId = 12345;
        bool result = _manager.SetGpuAffinity(processId, 1);

        Assert.True(result);
        var config = _manager.GetGpuAffinity(processId);
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.Equal(1, config.GpuIndex);
    }

    [Fact]
    public void GetGpuAffinity_NotBoundProcess_ReturnsNull()
    {
        Assert.Null(_manager.GetGpuAffinity(99999));
    }

    [Fact]
    public void SetEngineAffinity_InvalidProcessId_ReturnsFalse()
    {
        Assert.False(_manager.SetEngineAffinity(0, 1));
    }

    [Fact]
    public void SetEngineAffinity_NegativeEngineId_ReturnsFalse()
    {
        Assert.False(_manager.SetEngineAffinity(1, -1));
    }

    [Fact]
    public void SetEngineAffinity_ValidInputs_SetsEngine()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        int processId = 54321;
        bool result = _manager.SetEngineAffinity(processId, 2);

        Assert.True(result);
        var config = _manager.GetGpuAffinity(processId);
        Assert.NotNull(config);
        Assert.Equal(2, config.EngineId);
    }

    [Fact]
    public void SetEngineAffinity_OnExistingBinding_UpdatesEngine()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        int processId = 11111;
        _manager.SetGpuAffinity(processId, 0);
        _manager.SetEngineAffinity(processId, 3);

        var config = _manager.GetGpuAffinity(processId);
        Assert.NotNull(config);
        Assert.Equal(0, config.GpuIndex);
        Assert.Equal(3, config.EngineId);
    }

    [Fact]
    public void ClearGpuAffinity_ExistingBinding_ReturnsTrueAndRemoves()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        int processId = 22222;
        _manager.SetGpuAffinity(processId, 0);

        bool result = _manager.ClearGpuAffinity(processId);
        Assert.True(result);
        Assert.Null(_manager.GetGpuAffinity(processId));
    }

    [Fact]
    public void ClearGpuAffinity_NotBound_ReturnsFalse()
    {
        Assert.False(_manager.ClearGpuAffinity(99999));
    }

    [Fact]
    public void ClearGpuAffinity_InvalidProcessId_ReturnsFalse()
    {
        Assert.False(_manager.ClearGpuAffinity(0));
    }

    [Fact]
    public void GetGpuCount_SumsBothSdks()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(true);
        _mockAdl2.SetupGet(g => g.GpuCount).Returns(1);

        Assert.Equal(3, _manager.GetGpuCount());
    }

    [Fact]
    public void GetGpuCount_NoSdks_ReturnsZero()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(false);

        Assert.Equal(0, _manager.GetGpuCount());
    }

    [Fact]
    public void GetGpuName_NvApiAvailable_ReturnsName()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(1);
        _mockNvApi.Setup(g => g.GetGpuName(0)).Returns("NVIDIA GeForce RTX 4090");

        Assert.Equal("NVIDIA GeForce RTX 4090", _manager.GetGpuName(0));
    }

    [Fact]
    public void GetGpuName_Adl2Fallback_ReturnsName()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(true);
        _mockAdl2.SetupGet(g => g.GpuCount).Returns(1);
        _mockAdl2.Setup(g => g.GetGpuName(0)).Returns("AMD Radeon RX 7900 XTX");

        Assert.Equal("AMD Radeon RX 7900 XTX", _manager.GetGpuName(0));
    }

    [Fact]
    public void GetGpuName_NoSdk_ReturnsNull()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(false);

        Assert.Null(_manager.GetGpuName(0));
    }

    [Fact]
    public void ComputeGpuAffinityMask_ValidInput_ReturnsMask()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        long mask = _manager.ComputeGpuAffinityMask(0);
        Assert.NotEqual(0, mask);
    }

    [Fact]
    public void ComputeGpuAffinityMask_NegativeIndex_ReturnsZero()
    {
        long mask = _manager.ComputeGpuAffinityMask(-1);
        Assert.Equal(0, mask);
    }

    [Fact]
    public void SetGpuAffinity_MultipleProcesses_TracksIndependently()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);

        _manager.SetGpuAffinity(100, 0);
        _manager.SetGpuAffinity(200, 1);

        var config1 = _manager.GetGpuAffinity(100);
        var config2 = _manager.GetGpuAffinity(200);

        Assert.Equal(0, config1!.GpuIndex);
        Assert.Equal(1, config2!.GpuIndex);
    }
}