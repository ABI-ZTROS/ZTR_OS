using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Manages binding policies for automatic and manual process-to-CPU/GPU affinity assignment.
/// Provides auto-binding for game processes and policy priority resolution.
/// </summary>
public class BindingPolicy
{
    private readonly ProcessTracker _tracker;
    private readonly CpuAffinityManager _cpuManager;
    private readonly GpuAffinityManager _gpuManager;
    private readonly TopologyService _topologyService;
    private readonly List<BindingPolicyRule> _activePolicies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingPolicy"/> class.
    /// </summary>
    /// <param name="tracker">The process tracker for detecting and tracking processes.</param>
    /// <param name="cpuManager">The CPU affinity manager for applying CPU bindings.</param>
    /// <param name="gpuManager">The GPU affinity manager for applying GPU bindings.</param>
    /// <param name="topologyService">The topology service for hardware layout information.</param>
    public BindingPolicy(
        ProcessTracker tracker,
        CpuAffinityManager cpuManager,
        GpuAffinityManager gpuManager,
        TopologyService topologyService)
    {
        _tracker = tracker;
        _cpuManager = cpuManager;
        _gpuManager = gpuManager;
        _topologyService = topologyService;
    }

    /// <summary>
    /// Gets the currently active binding policies.
    /// </summary>
    public IReadOnlyList<BindingPolicyRule> GetActivePolicies()
    {
        return _activePolicies.AsReadOnly();
    }

    /// <summary>
    /// Auto-binds detected game processes to optimal CPU cores and GPUs.
    /// Games are identified by the process tracker's known game process list.
    /// Each game process is bound to dedicated cores for optimal performance.
    /// </summary>
    /// <returns>The number of processes that were successfully bound.</returns>
    public int AutoBindGameProcesses()
    {
        var gameProcesses = _tracker.GetAllProcesses()
            .Where(p => _tracker.IsGameProcess(p.ProcessName))
            .ToList();

        int boundCount = 0;
        int totalCores = CpuAffinityManager.GetLogicalProcessorCount();
        int coresPerGame = Math.Max(2, totalCores / Math.Max(1, gameProcesses.Count));

        for (int i = 0; i < gameProcesses.Count; i++)
        {
            var gameProcess = gameProcesses[i];

            try
            {
                int startCore = (i * coresPerGame) % totalCores;
                int[] coreIndices = Enumerable.Range(startCore, Math.Min(coresPerGame, totalCores - startCore)).ToArray();

                long affinityMask = CpuAffinityManager.CreateMask(coreIndices);

                var binding = new ProcessBinding
                {
                    ProcessId = gameProcess.ProcessId,
                    ProcessName = gameProcess.ProcessName,
                    MainWindowTitle = gameProcess.MainWindowTitle,
                    CpuAffinity = new CpuAffinityConfig
                    {
                        Enabled = true,
                        AffinityMask = affinityMask,
                        CoreIndices = coreIndices,
                        UseNumaNode = false
                    },
                    GpuAffinity = new GpuAffinityConfig
                    {
                        Enabled = true,
                        GpuIndex = 0,
                        EngineId = 0
                    },
                    Strategy = BindingStrategy.AutoGame
                };

                _cpuManager.SetAffinity(gameProcess.ProcessId, affinityMask);
                _gpuManager.SetGpuAffinity(gameProcess.ProcessId, 0);
                _tracker.TrackProcess(binding);

                var rule = new BindingPolicyRule
                {
                    Name = $"AutoBind_{gameProcess.ProcessName}_{gameProcess.ProcessId}",
                    Strategy = BindingStrategy.AutoGame,
                    Priority = 100,
                    TargetProcessName = gameProcess.ProcessName,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Binding = binding
                };

                _activePolicies.Add(rule);
                boundCount++;
            }
            catch
            {
            }
        }

        return boundCount;
    }

    /// <summary>
    /// Creates a manual binding policy rule from the specified binding configuration.
    /// </summary>
    /// <param name="binding">The process binding configuration to create a policy for.</param>
    /// <returns>The created <see cref="BindingPolicyRule"/>.</returns>
    public BindingPolicyRule CreateManualPolicy(ProcessBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        var rule = new BindingPolicyRule
        {
            Name = $"Manual_{binding.ProcessName}_{binding.ProcessId}",
            Strategy = BindingStrategy.Manual,
            Priority = 50,
            TargetProcessName = binding.ProcessName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Binding = binding
        };

        _activePolicies.Add(rule);

        if (binding.CpuAffinity.Enabled)
        {
            _cpuManager.SetAffinity(binding.ProcessId, binding.CpuAffinity.AffinityMask);
        }

        if (binding.GpuAffinity.Enabled)
        {
            _gpuManager.SetGpuAffinity(binding.ProcessId, binding.GpuAffinity.GpuIndex);
        }

        _tracker.TrackProcess(binding);

        return rule;
    }

    /// <summary>
    /// Creates a manual binding policy with a specified priority.
    /// </summary>
    /// <param name="binding">The process binding configuration.</param>
    /// <param name="priority">The priority value (higher takes precedence).</param>
    /// <returns>The created <see cref="BindingPolicyRule"/>.</returns>
    public BindingPolicyRule CreateManualPolicy(ProcessBinding binding, int priority)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        var rule = new BindingPolicyRule
        {
            Name = $"Manual_{binding.ProcessName}_{binding.ProcessId}",
            Strategy = BindingStrategy.Manual,
            Priority = priority,
            TargetProcessName = binding.ProcessName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Binding = binding
        };

        _activePolicies.Add(rule);

        if (binding.CpuAffinity.Enabled)
        {
            _cpuManager.SetAffinity(binding.ProcessId, binding.CpuAffinity.AffinityMask);
        }

        if (binding.GpuAffinity.Enabled)
        {
            _gpuManager.SetGpuAffinity(binding.ProcessId, binding.GpuAffinity.GpuIndex);
        }

        _tracker.TrackProcess(binding);

        return rule;
    }

    /// <summary>
    /// Applies all currently active binding policies, resolving conflicts by priority.
    /// Higher-priority policies take precedence over lower-priority ones.
    /// </summary>
    /// <returns>The number of policy applications that succeeded.</returns>
    public int ApplyAllPolicies()
    {
        var activeRules = _activePolicies
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToList();

        int appliedCount = 0;
        var appliedProcessIds = new HashSet<int>();

        foreach (var rule in activeRules)
        {
            if (appliedProcessIds.Contains(rule.Binding.ProcessId))
                continue;

            try
            {
                if (rule.Binding.CpuAffinity.Enabled)
                {
                    _cpuManager.SetAffinity(rule.Binding.ProcessId, rule.Binding.CpuAffinity.AffinityMask);
                }

                if (rule.Binding.GpuAffinity.Enabled)
                {
                    _gpuManager.SetGpuAffinity(rule.Binding.ProcessId, rule.Binding.GpuAffinity.GpuIndex);
                }

                appliedProcessIds.Add(rule.Binding.ProcessId);
                appliedCount++;
            }
            catch
            {
            }
        }

        return appliedCount;
    }

    /// <summary>
    /// Evaluates and resolves policy conflicts for a specific process.
    /// Returns the highest-priority active rule that targets the process.
    /// </summary>
    /// <param name="processId">The process identifier to evaluate policies for.</param>
    /// <returns>The winning <see cref="BindingPolicyRule"/>, or null if no active policy targets the process.</returns>
    public BindingPolicyRule? ResolvePolicyConflict(int processId)
    {
        return _activePolicies
            .Where(r => r.IsActive && r.Binding.ProcessId == processId)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();
    }

    /// <summary>
    /// Disables a policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy to disable.</param>
    /// <returns>True if the policy was found and disabled; otherwise false.</returns>
    public bool DisablePolicy(string policyName)
    {
        var rule = _activePolicies.FirstOrDefault(r => r.Name == policyName);
        if (rule == null)
            return false;

        rule.IsActive = false;
        return true;
    }

    /// <summary>
    /// Enables a previously disabled policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy to enable.</param>
    /// <returns>True if the policy was found and enabled; otherwise false.</returns>
    public bool EnablePolicy(string policyName)
    {
        var rule = _activePolicies.FirstOrDefault(r => r.Name == policyName);
        if (rule == null)
            return false;

        rule.IsActive = true;
        return true;
    }

    /// <summary>
    /// Removes a policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy to remove.</param>
    /// <returns>True if the policy was found and removed; otherwise false.</returns>
    public bool RemovePolicy(string policyName)
    {
        var rule = _activePolicies.FirstOrDefault(r => r.Name == policyName);
        if (rule == null)
            return false;

        _activePolicies.Remove(rule);
        return true;
    }

    /// <summary>
    /// Clears all active policies.
    /// </summary>
    public void ClearAllPolicies()
    {
        _activePolicies.Clear();
    }
}

/// <summary>
/// Represents a binding policy rule with priority and targeting information.
/// </summary>
public class BindingPolicyRule
{
    /// <summary>
    /// Gets or sets the unique name of this policy rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the binding strategy used by this rule.
    /// </summary>
    public BindingStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets the priority of this rule. Higher values take precedence.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the target process name pattern for this rule.
    /// </summary>
    public string TargetProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this rule is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date and time the rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the binding configuration associated with this rule.
    /// </summary>
    public ProcessBinding Binding { get; set; } = new();
}