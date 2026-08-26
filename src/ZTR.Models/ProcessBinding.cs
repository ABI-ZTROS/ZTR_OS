namespace ZTR.Models;

public class ProcessBinding
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string MainWindowTitle { get; set; } = string.Empty;
    public CpuAffinityConfig CpuAffinity { get; set; } = new();
    public GpuAffinityConfig GpuAffinity { get; set; } = new();
    public BindingStrategy Strategy { get; set; }
}

public class CpuAffinityConfig
{
    public bool Enabled { get; set; }
    public long AffinityMask { get; set; }
    public int[] CoreIndices { get; set; } = Array.Empty<int>();
    public bool UseNumaNode { get; set; }
    public int NumaNodeId { get; set; }
}

public class GpuAffinityConfig
{
    public bool Enabled { get; set; }
    public int GpuIndex { get; set; }
    public int EngineId { get; set; }
}

public enum BindingStrategy
{
    Manual,
    MlpDriven,
    AutoGame,
    AutoBalanced
}

public class CpuTopology
{
    public int TotalCores { get; set; }
    public int TotalLogicalProcessors { get; set; }
    public int NumaNodeCount { get; set; }
    public IReadOnlyList<CpuNumaNode> NumaNodes { get; set; } = Array.Empty<CpuNumaNode>();
    public IReadOnlyList<CpuCacheLevel> CacheLevels { get; set; } = Array.Empty<CpuCacheLevel>();
}

public class CpuNumaNode
{
    public int NodeId { get; set; }
    public long AffinityMask { get; set; }
    public int[] ProcessorIndices { get; set; } = Array.Empty<int>();
}

public class CpuCacheLevel
{
    public int Level { get; set; }
    public int SizeKB { get; set; }
    public int Associativity { get; set; }
    public int SharedProcessors { get; set; }
}

public class GpuTopology
{
    public int GpuCount { get; set; }
    public IReadOnlyList<GpuInfo> Gpus { get; set; } = Array.Empty<GpuInfo>();
}

public class GpuInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public long VramMB { get; set; }
    public int EngineCount { get; set; }
}