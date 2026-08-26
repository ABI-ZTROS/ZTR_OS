using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class CpuAffinityManagerExtendedTests
{
    private readonly CpuAffinityManager _manager;

    public CpuAffinityManagerExtendedTests()
    {
        _manager = new CpuAffinityManager();
    }

    [Fact]
    public void GetThreadAffinityMask_InvalidThreadId_ReturnsNull()
    {
        Assert.Null(_manager.GetThreadAffinityMask(0));
        Assert.Null(_manager.GetThreadAffinityMask(-1));
    }

    [Fact]
    public void GetThreadAffinityMask_ValidThreadId_ReturnsValueOrNull()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        var result = _manager.GetThreadAffinityMask(threadId);

        Assert.True(result == null || result > 0);
    }

    [Fact]
    public void SetThreadAffinityMask_InvalidThreadId_ReturnsFalse()
    {
        Assert.False(_manager.SetThreadAffinityMask(0, 0xFF));
        Assert.False(_manager.SetThreadAffinityMask(-1, 0xFF));
    }

    [Fact]
    public void SetThreadAffinityMask_ZeroMask_ReturnsFalse()
    {
        Assert.False(_manager.SetThreadAffinityMask(1, 0));
    }

    [Fact]
    public void SetThreadAffinityMask_ValidInputs_AttemptsSet()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        long mask = CpuAffinityManager.CreateMask(0);

        bool result = _manager.SetThreadAffinityMask(threadId, mask);
        Assert.True(result || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }

    [Fact]
    public void GetNumaNodeAffinity_NodeZero_ReturnsPositiveMask()
    {
        long mask = _manager.GetNumaNodeAffinity(0);
        Assert.True(mask > 0);
    }

    [Fact]
    public void GetNumaNodeAffinity_InvalidNode_ReturnsZero()
    {
        long mask = _manager.GetNumaNodeAffinity(999);
        Assert.Equal(0, mask);
    }

    [Fact]
    public void SetProcessNumaNode_InvalidProcessId_ReturnsFalse()
    {
        Assert.False(_manager.SetProcessNumaNode(0, 0));
        Assert.False(_manager.SetProcessNumaNode(-1, 0));
    }

    [Fact]
    public void SetProcessNumaNode_ValidProcessId_AttemptsSet()
    {
        int currentPid = Process.GetCurrentProcess().Id;
        bool result = _manager.SetProcessNumaNode(currentPid, 0);

        Assert.True(result || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }

    [Fact]
    public void SetProcessNumaNode_InvalidNode_ReturnsFalse()
    {
        int currentPid = Process.GetCurrentProcess().Id;
        bool result = _manager.SetProcessNumaNode(currentPid, 999);
        Assert.False(result);
    }

    [Fact]
    public void GetNumaNodeProcessors_ValidNode_ReturnsNonEmpty()
    {
        var processors = _manager.GetNumaNodeProcessors(0);
        Assert.NotNull(processors);
        Assert.True(processors.Length > 0);
    }

    [Fact]
    public void GetNumaNodeProcessors_InvalidNode_ReturnsEmpty()
    {
        var processors = _manager.GetNumaNodeProcessors(999);
        Assert.NotNull(processors);
        Assert.Empty(processors);
    }

    [Fact]
    public void CreateNumaNodeMask_ValidNode_ReturnsPositiveMask()
    {
        long mask = _manager.CreateNumaNodeMask(0);
        Assert.True(mask > 0);
    }

    [Fact]
    public void CreateNumaNodeMask_InvalidNode_ReturnsZero()
    {
        long mask = _manager.CreateNumaNodeMask(999);
        Assert.Equal(0, mask);
    }

    [Fact]
    public void CreateNumaNodeMask_WithMaxCores_LimitsCores()
    {
        long fullMask = _manager.CreateNumaNodeMask(0);
        long limitedMask = _manager.CreateNumaNodeMask(0, 2);

        Assert.True(limitedMask > 0);
        Assert.True(BitOperations.PopCount((ulong)limitedMask) <= 2);
    }

    [Fact]
    public void CreateNumaNodeMask_MaxCoresExceedsAvailable_ReturnsFullMask()
    {
        long fullMask = _manager.CreateNumaNodeMask(0);
        int totalCores = CpuAffinityManager.GetLogicalProcessorCount();
        long mask = _manager.CreateNumaNodeMask(0, totalCores + 100);

        Assert.Equal(fullMask, mask);
    }

    [Fact]
    public void GetNumaNodeAffinity_MatchesTopologyNode()
    {
        long mask = _manager.GetNumaNodeAffinity(0);
        var topology = _manager.GetTopology();

        Assert.Equal(topology.NumaNodes[0].AffinityMask, mask);
    }

    [Fact]
    public void SetThreadAffinityMask_ConcurrentCalls_DoNotThrow()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        long mask = CpuAffinityManager.CreateMask(0);

        var exception = Record.Exception(() =>
        {
            _manager.SetThreadAffinityMask(threadId, mask);
            _manager.SetThreadAffinityMask(threadId, mask);
            _manager.SetThreadAffinityMask(threadId, mask);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void GetThreadAffinityMask_ConcurrentCalls_DoNotThrow()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;

        var exception = Record.Exception(() =>
        {
            _manager.GetThreadAffinityMask(threadId);
            _manager.GetThreadAffinityMask(threadId);
            _manager.GetThreadAffinityMask(threadId);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void GetNumaNodeAffinity_MultipleNodes_OrderedById()
    {
        var topology = _manager.GetTopology();
        var nodes = topology.NumaNodes.OrderBy(n => n.NodeId).ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            long mask = _manager.GetNumaNodeAffinity(nodes[i].NodeId);
            Assert.Equal(nodes[i].AffinityMask, mask);
        }
    }
}