using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class TopologyServiceTests
{
    private readonly Mock<INvApiGpu> _mockNvApi;
    private readonly Mock<IAdl2Gpu> _mockAdl2;
    private readonly Mock<IWmiQueryService> _mockQueryService;
    private readonly TopologyService _service;

    public TopologyServiceTests()
    {
        _mockNvApi = new Mock<INvApiGpu>();
        _mockAdl2 = new Mock<IAdl2Gpu>();
        _mockQueryService = new Mock<IWmiQueryService>();
        _service = new TopologyService(_mockNvApi.Object, _mockAdl2.Object, _mockQueryService.Object);
    }

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        var service = new TopologyService();
        Assert.NotNull(service);
    }

    [Fact]
    public void GetCpuTopology_ReturnsValidTopology()
    {
        var topology = _service.GetCpuTopology();

        Assert.NotNull(topology);
        Assert.True(topology.TotalLogicalProcessors > 0);
        Assert.True(topology.TotalCores >= 0);
        Assert.NotNull(topology.NumaNodes);
        Assert.True(topology.NumaNodeCount >= 1);
    }

    [Fact]
    public void GetCpuTopology_NumaNodes_HaveValidMasks()
    {
        var topology = _service.GetCpuTopology();

        foreach (var node in topology.NumaNodes)
        {
            Assert.True(node.AffinityMask > 0);
            Assert.NotNull(node.ProcessorIndices);
            Assert.True(node.ProcessorIndices.Length > 0);
        }
    }

    [Fact]
    public void GetCpuTopology_CacheLevels_HaveValidStructure()
    {
        var topology = _service.GetCpuTopology();

        Assert.NotNull(topology.CacheLevels);
        Assert.True(topology.CacheLevels.Count > 0);

        foreach (var level in topology.CacheLevels)
        {
            Assert.True(level.Level >= 1 && level.Level <= 3);
            Assert.True(level.SizeKB > 0);
        }
    }

    [Fact]
    public void GetCpuTopology_CachesResult()
    {
        var first = _service.GetCpuTopology();
        var second = _service.GetCpuTopology();

        Assert.Same(first, second);
    }

    [Fact]
    public void InvalidateCache_ClearsTopologyCache()
    {
        var first = _service.GetCpuTopology();
        _service.InvalidateCache();
        var second = _service.GetCpuTopology();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void GetGpuTopology_NoGpus_ReturnsEmptyGpuList()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(false);

        var topology = _service.GetGpuTopology();

        Assert.NotNull(topology);
        Assert.Equal(0, topology.GpuCount);
        Assert.Empty(topology.Gpus);
    }

    [Fact]
    public void GetGpuTopology_NvApiAvailable_ReturnsGpus()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(2);
        _mockNvApi.Setup(g => g.GetGpuName(0)).Returns("NVIDIA GPU 0");
        _mockNvApi.Setup(g => g.GetGpuName(1)).Returns("NVIDIA GPU 1");
        _mockNvApi.Setup(g => g.GetVramInfo(0)).Returns((8192L, 16384L));
        _mockNvApi.Setup(g => g.GetVramInfo(1)).Returns((4096L, 8192L));
        _mockNvApi.Setup(g => g.GetClockInfo(0)).Returns((1800, 5000));
        _mockNvApi.Setup(g => g.GetClockInfo(1)).Returns((1600, 4500));

        var topology = _service.GetGpuTopology();

        Assert.Equal(2, topology.GpuCount);
        Assert.Equal(2, topology.Gpus.Count);
        Assert.Equal("NVIDIA GPU 0", topology.Gpus[0].Name);
        Assert.Equal("NVIDIA GPU 1", topology.Gpus[1].Name);
        Assert.Equal(16384, topology.Gpus[0].VramMB);
    }

    [Fact]
    public void GetGpuTopology_Adl2Available_ReturnsGpus()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(false);
        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(true);
        _mockAdl2.SetupGet(g => g.GpuCount).Returns(1);
        _mockAdl2.Setup(g => g.GetGpuName(0)).Returns("AMD GPU 0");
        _mockAdl2.Setup(g => g.GetVramInfo(0)).Returns((2048L, 8192L));

        var topology = _service.GetGpuTopology();

        Assert.Equal(1, topology.GpuCount);
        Assert.Single(topology.Gpus);
        Assert.Equal("AMD GPU 0", topology.Gpus[0].Name);
    }

    [Fact]
    public void GetGpuTopology_BothSdksAvailable_ReturnsCombinedGpus()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(1);
        _mockNvApi.Setup(g => g.GetGpuName(0)).Returns("NVIDIA GPU");
        _mockNvApi.Setup(g => g.GetVramInfo(0)).Returns((2048L, 4096L));
        _mockNvApi.Setup(g => g.GetClockInfo(0)).Returns((1500, 4000));

        _mockAdl2.SetupGet(g => g.IsAvailable).Returns(true);
        _mockAdl2.SetupGet(g => g.GpuCount).Returns(1);
        _mockAdl2.Setup(g => g.GetGpuName(0)).Returns("AMD GPU");
        _mockAdl2.Setup(g => g.GetVramInfo(0)).Returns((1024L, 2048L));

        var topology = _service.GetGpuTopology();

        Assert.Equal(2, topology.GpuCount);
        Assert.Equal(2, topology.Gpus.Count);
    }

    [Fact]
    public void GetGpuTopology_CachesResult()
    {
        _mockNvApi.SetupGet(g => g.IsAvailable).Returns(true);
        _mockNvApi.SetupGet(g => g.GpuCount).Returns(1);
        _mockNvApi.Setup(g => g.GetGpuName(0)).Returns("Test GPU");
        _mockNvApi.Setup(g => g.GetVramInfo(0)).Returns((0L, 1024L));
        _mockNvApi.Setup(g => g.GetClockInfo(0)).Returns((1000, 3000));

        var first = _service.GetGpuTopology();
        var second = _service.GetGpuTopology();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetNumaNodes_ReturnsNodesFromTopology()
    {
        var nodes = _service.GetNumaNodes();

        Assert.NotNull(nodes);
        Assert.True(nodes.Count >= 1);
    }

    [Fact]
    public void GetCacheLevels_ReturnsLevelsFromTopology()
    {
        var levels = _service.GetCacheLevels();

        Assert.NotNull(levels);
        Assert.True(levels.Count >= 1);
    }

    [Fact]
    public void DetectNumaNodes_ReturnsAtLeastOneNode()
    {
        int processorCount = Environment.ProcessorCount;
        var nodes = _service.DetectNumaNodes(processorCount);

        Assert.NotNull(nodes);
        Assert.True(nodes.Count >= 1);
        Assert.Equal(0, nodes[0].NodeId);
        Assert.True(nodes[0].AffinityMask > 0);
    }

    [Fact]
    public void DetectCacheLevels_WithWmiQuery_ReturnsLevels()
    {
        var mockResults = new List<IDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Level", "1" },
                { "Size", "32" },
                { "Associativity", "8" }
            },
            new Dictionary<string, object>
            {
                { "Level", "2" },
                { "Size", "512" },
                { "Associativity", "8" }
            }
        };

        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Returns(mockResults);

        var levels = _service.DetectCacheLevels();

        Assert.True(levels.Count >= 2);
        Assert.Equal(1, levels[0].Level);
        Assert.Equal(32, levels[0].SizeKB);
        Assert.Equal(2, levels[1].Level);
    }

    [Fact]
    public void DetectCacheLevels_WhenWmiFails_FallsBackToDefaults()
    {
        _mockQueryService.Setup(q => q.ExecuteQuery(It.IsAny<string>()))
            .Throws(new Exception("WMI unavailable"));

        var levels = _service.DetectCacheLevels();

        Assert.True(levels.Count >= 3);
        Assert.Contains(levels, l => l.Level == 1);
        Assert.Contains(levels, l => l.Level == 2);
        Assert.Contains(levels, l => l.Level == 3);
    }
}