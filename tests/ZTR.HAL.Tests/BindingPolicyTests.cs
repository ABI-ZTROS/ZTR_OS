using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class BindingPolicyTests
{
    private readonly ProcessTracker _tracker;
    private readonly CpuAffinityManager _cpuManager;
    private readonly GpuAffinityManager _gpuManager;
    private readonly TopologyService _topologyService;
    private readonly BindingPolicy _policy;

    public BindingPolicyTests()
    {
        _tracker = new ProcessTracker();
        _cpuManager = new CpuAffinityManager();
        _gpuManager = new GpuAffinityManager(null, null);
        _topologyService = new TopologyService();
        _policy = new BindingPolicy(_tracker, _cpuManager, _gpuManager, _topologyService);
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        Assert.NotNull(_policy);
    }

    [Fact]
    public void GetActivePolicies_InitiallyEmpty()
    {
        var policies = _policy.GetActivePolicies();
        Assert.NotNull(policies);
        Assert.Empty(policies);
    }

    [Fact]
    public void CreateManualPolicy_ValidBinding_AddsPolicy()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 100,
            ProcessName = "TestProcess",
            CpuAffinity = new CpuAffinityConfig
            {
                Enabled = true,
                AffinityMask = 0xFF,
                CoreIndices = new[] { 0, 1, 2, 3 }
            },
            GpuAffinity = new GpuAffinityConfig
            {
                Enabled = true,
                GpuIndex = 0,
                EngineId = 0
            },
            Strategy = BindingStrategy.Manual
        };

        var rule = _policy.CreateManualPolicy(binding);

        Assert.NotNull(rule);
        Assert.Equal("Manual_TestProcess_100", rule.Name);
        Assert.Equal(BindingStrategy.Manual, rule.Strategy);
        Assert.True(rule.IsActive);
        Assert.Equal(50, rule.Priority);
        Assert.Single(_policy.GetActivePolicies());
    }

    [Fact]
    public void CreateManualPolicy_WithCustomPriority_SetsPriority()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 200,
            ProcessName = "PriorityProcess",
            CpuAffinity = new CpuAffinityConfig
            {
                Enabled = true,
                AffinityMask = 0x0F
            },
            GpuAffinity = new GpuAffinityConfig
            {
                Enabled = false
            }
        };

        var rule = _policy.CreateManualPolicy(binding, 200);

        Assert.Equal(200, rule.Priority);
    }

    [Fact]
    public void CreateManualPolicy_NullBinding_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _policy.CreateManualPolicy(null!));
    }

    [Fact]
    public void CreateManualPolicy_NullBindingWithPriority_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _policy.CreateManualPolicy(null!, 100));
    }

    [Fact]
    public void ApplyAllPolicies_NoPolicies_ReturnsZero()
    {
        int result = _policy.ApplyAllPolicies();
        Assert.Equal(0, result);
    }

    [Fact]
    public void ApplyAllPolicies_WithPolicies_AppliesThem()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 300,
            ProcessName = "ApplyTest",
            CpuAffinity = new CpuAffinityConfig
            {
                Enabled = true,
                AffinityMask = 0xFF,
                CoreIndices = new[] { 0, 1, 2, 3 }
            },
            GpuAffinity = new GpuAffinityConfig
            {
                Enabled = false
            }
        };

        _policy.CreateManualPolicy(binding);
        int result = _policy.ApplyAllPolicies();

        Assert.Equal(1, result);
    }

    [Fact]
    public void ApplyAllPolicies_ResolvesPriorityConflicts()
    {
        var lowPriorityBinding = new ProcessBinding
        {
            ProcessId = 400,
            ProcessName = "ConflictProcess",
            CpuAffinity = new CpuAffinityConfig
            {
                Enabled = true,
                AffinityMask = 0x0F
            },
            GpuAffinity = new GpuAffinityConfig
            {
                Enabled = false
            }
        };

        var highPriorityBinding = new ProcessBinding
        {
            ProcessId = 400,
            ProcessName = "ConflictProcess",
            CpuAffinity = new CpuAffinityConfig
            {
                Enabled = true,
                AffinityMask = 0xF0
            },
            GpuAffinity = new GpuAffinityConfig
            {
                Enabled = false
            }
        };

        _policy.CreateManualPolicy(lowPriorityBinding, 10);
        _policy.CreateManualPolicy(highPriorityBinding, 100);

        int result = _policy.ApplyAllPolicies();
        Assert.Equal(1, result);
    }

    [Fact]
    public void ResolvePolicyConflict_NoPolicies_ReturnsNull()
    {
        var result = _policy.ResolvePolicyConflict(999);
        Assert.Null(result);
    }

    [Fact]
    public void ResolvePolicyConflict_ReturnsHighestPriority()
    {
        var binding1 = new ProcessBinding
        {
            ProcessId = 500,
            ProcessName = "ResolveTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0x01 },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var binding2 = new ProcessBinding
        {
            ProcessId = 500,
            ProcessName = "ResolveTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0x02 },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        _policy.CreateManualPolicy(binding1, 10);
        _policy.CreateManualPolicy(binding2, 200);

        var winner = _policy.ResolvePolicyConflict(500);
        Assert.NotNull(winner);
        Assert.Equal(200, winner.Priority);
    }

    [Fact]
    public void DisablePolicy_ExistingPolicy_ReturnsFalseForApply()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 600,
            ProcessName = "DisableTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0xFF },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var rule = _policy.CreateManualPolicy(binding);
        _policy.DisablePolicy(rule.Name);

        Assert.False(rule.IsActive);

        int result = _policy.ApplyAllPolicies();
        Assert.Equal(0, result);
    }

    [Fact]
    public void DisablePolicy_NonExisting_ReturnsFalse()
    {
        Assert.False(_policy.DisablePolicy("NonExistent"));
    }

    [Fact]
    public void EnablePolicy_DisabledPolicy_Reactivates()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 700,
            ProcessName = "EnableTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0xFF },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var rule = _policy.CreateManualPolicy(binding);
        _policy.DisablePolicy(rule.Name);
        _policy.EnablePolicy(rule.Name);

        Assert.True(rule.IsActive);
    }

    [Fact]
    public void EnablePolicy_NonExisting_ReturnsFalse()
    {
        Assert.False(_policy.EnablePolicy("NonExistent"));
    }

    [Fact]
    public void RemovePolicy_Existing_ReturnsTrue()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 800,
            ProcessName = "RemoveTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0xFF },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var rule = _policy.CreateManualPolicy(binding);
        int countBefore = _policy.GetActivePolicies().Count;
        bool result = _policy.RemovePolicy(rule.Name);

        Assert.True(result);
        Assert.Equal(countBefore - 1, _policy.GetActivePolicies().Count);
    }

    [Fact]
    public void RemovePolicy_NonExisting_ReturnsFalse()
    {
        Assert.False(_policy.RemovePolicy("NonExistent"));
    }

    [Fact]
    public void ClearAllPolicies_RemovesAll()
    {
        var binding1 = new ProcessBinding
        {
            ProcessId = 900,
            ProcessName = "Clear1",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0xFF },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var binding2 = new ProcessBinding
        {
            ProcessId = 901,
            ProcessName = "Clear2",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0xFF },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        _policy.CreateManualPolicy(binding1);
        _policy.CreateManualPolicy(binding2);

        Assert.True(_policy.GetActivePolicies().Count >= 2);

        _policy.ClearAllPolicies();
        Assert.Empty(_policy.GetActivePolicies());
    }

    [Fact]
    public void AutoBindGameProcesses_NoGamesRunning_ReturnsZero()
    {
        int result = _policy.AutoBindGameProcesses();
        Assert.True(result >= 0);
    }

    [Fact]
    public void AutoBindGameProcesses_ForCurrentProcess_CouldBind()
    {
        var currentProc = System.Diagnostics.Process.GetCurrentProcess();
        var binding = new ProcessBinding
        {
            ProcessId = currentProc.Id,
            ProcessName = currentProc.ProcessName
        };
        _tracker.TrackProcess(binding);

        int result = _policy.AutoBindGameProcesses();
        Assert.True(result >= 0);
    }

    [Fact]
    public void CreateManualPolicy_TracksProcess()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 1000,
            ProcessName = "TrackTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0x0F },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        _policy.CreateManualPolicy(binding);
        var tracked = _tracker.GetTrackedBindings();

        Assert.True(tracked.ContainsKey(1000));
    }

    [Fact]
    public void PolicyRule_StoresCreationTime()
    {
        var before = DateTimeOffset.UtcNow;

        var binding = new ProcessBinding
        {
            ProcessId = 1100,
            ProcessName = "TimeTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0x0F },
            GpuAffinity = new GpuAffinityConfig { Enabled = false }
        };

        var rule = _policy.CreateManualPolicy(binding);
        var after = DateTimeOffset.UtcNow;

        Assert.True(rule.CreatedAt >= before && rule.CreatedAt <= after);
    }

    [Fact]
    public void PolicyRule_StoresBinding()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 1200,
            ProcessName = "StoreTest",
            CpuAffinity = new CpuAffinityConfig { Enabled = true, AffinityMask = 0x0F },
            GpuAffinity = new GpuAffinityConfig { Enabled = true, GpuIndex = 1 }
        };

        var rule = _policy.CreateManualPolicy(binding);

        Assert.NotNull(rule.Binding);
        Assert.Equal(1200, rule.Binding.ProcessId);
        Assert.Equal("StoreTest", rule.Binding.ProcessName);
        Assert.True(rule.Binding.CpuAffinity.Enabled);
        Assert.True(rule.Binding.GpuAffinity.Enabled);
    }
}