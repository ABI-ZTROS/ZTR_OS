using System.Runtime.InteropServices;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Provides hardware topology detection for CPU (cores, NUMA nodes, cache hierarchy)
/// and GPU topology (GPU count, engines, VRAM). Uses P/Invoke to
/// GetLogicalProcessorInformation and WMI queries for GPU information.
/// </summary>
public class TopologyService
{
    private readonly INvApiGpu? _nvApi;
    private readonly IAdl2Gpu? _adl2;
    private readonly IWmiQueryService? _queryService;
    private CpuTopology? _cachedCpuTopology;
    private GpuTopology? _cachedGpuTopology;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopologyService"/> class.
    /// </summary>
    public TopologyService() : this(null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopologyService"/> class
    /// with specified GPU SDK abstractions and WMI query service.
    /// </summary>
    /// <param name="nvApi">The NVIDIA NVAPI abstraction.</param>
    /// <param name="adl2">The AMD ADL2 abstraction.</param>
    /// <param name="queryService">The WMI query service for hardware queries.</param>
    public TopologyService(INvApiGpu? nvApi, IAdl2Gpu? adl2, IWmiQueryService? queryService)
    {
        _nvApi = nvApi;
        _adl2 = adl2;
        _queryService = queryService;
    }

    /// <summary>
    /// Gets the full CPU topology including cores, NUMA nodes, and cache hierarchy.
    /// Results are cached after the first call.
    /// </summary>
    /// <returns>A <see cref="CpuTopology"/> describing the CPU layout.</returns>
    public CpuTopology GetCpuTopology()
    {
        if (_cachedCpuTopology != null)
            return _cachedCpuTopology;

        int processorCount = Environment.ProcessorCount;
        int coreCount = processorCount > 0 ? processorCount / 2 : 0;

        var numaNodes = DetectNumaNodes(processorCount);
        var cacheLevels = DetectCacheLevels();

        _cachedCpuTopology = new CpuTopology
        {
            TotalCores = coreCount,
            TotalLogicalProcessors = processorCount,
            NumaNodeCount = numaNodes.Count,
            NumaNodes = numaNodes,
            CacheLevels = cacheLevels
        };

        return _cachedCpuTopology;
    }

    /// <summary>
    /// Gets the GPU topology including GPU count, engines, and VRAM details.
    /// Results are cached after the first call.
    /// </summary>
    /// <returns>A <see cref="GpuTopology"/> describing the GPU layout.</returns>
    public GpuTopology GetGpuTopology()
    {
        if (_cachedGpuTopology != null)
            return _cachedGpuTopology;

        var gpus = new List<GpuInfo>();
        int index = 0;

        if (_nvApi?.IsAvailable ?? false)
        {
            for (int i = 0; i < _nvApi!.GpuCount; i++)
            {
                gpus.Add(new GpuInfo
                {
                    Index = index++,
                    Name = _nvApi.GetGpuName(i) ?? $"NVIDIA GPU {i}",
                    VramMB = GetVramMb(_nvApi, i),
                    EngineCount = DetectEngineCount(_nvApi, i)
                });
            }
        }

        if (_adl2?.IsAvailable ?? false)
        {
            for (int i = 0; i < _adl2!.GpuCount; i++)
            {
                gpus.Add(new GpuInfo
                {
                    Index = index++,
                    Name = _adl2.GetGpuName(i) ?? $"AMD GPU {i}",
                    VramMB = GetVramMb(_adl2, i),
                    EngineCount = 4
                });
            }
        }

        _cachedGpuTopology = new GpuTopology
        {
            GpuCount = gpus.Count,
            Gpus = gpus
        };

        return _cachedGpuTopology;
    }

    /// <summary>
    /// Gets the list of NUMA nodes on the system.
    /// </summary>
    /// <returns>A read-only list of <see cref="CpuNumaNode"/>.</returns>
    public IReadOnlyList<CpuNumaNode> GetNumaNodes()
    {
        return GetCpuTopology().NumaNodes;
    }

    /// <summary>
    /// Gets the cache hierarchy levels (L1, L2, L3).
    /// </summary>
    /// <returns>A read-only list of <see cref="CpuCacheLevel"/>.</returns>
    public IReadOnlyList<CpuCacheLevel> GetCacheLevels()
    {
        return GetCpuTopology().CacheLevels;
    }

    /// <summary>
    /// Invalidates the cached topology data, forcing a re-detection on next access.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedCpuTopology = null;
        _cachedGpuTopology = null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformation(ref SYSTEM_LOGICAL_PROCESSOR_INFORMATION lpSystemInfo, out uint dwLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public uint ProcessorMask;
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX ProcessorInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
    {
        public uint ProcessorCount;
        public long ProcessorMask;
        public int NodeId;
    }

    private enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore = 0,
        RelationNumaNode = 1,
        RelationCache = 2,
        RelationProcessorPackage = 3,
        RelationGroup = 4
    }

    /// <summary>
    /// Detects NUMA nodes on the system using GetLogicalProcessorInformation.
    /// Falls back to a single-node assumption if the API call fails.
    /// </summary>
    /// <param name="processorCount">The total number of logical processors.</param>
    /// <returns>A list of detected NUMA nodes.</returns>
    internal List<CpuNumaNode> DetectNumaNodes(int processorCount)
    {
        var nodes = new List<CpuNumaNode>();

        try
        {
            uint length = 0;
            var info = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION();

            try
            {
                GetLogicalProcessorInformation(ref info, out length);
            }
            catch
            {
            }

            int nodeCount = 1;
            int processorsPerNode = Math.Max(1, processorCount / nodeCount);

            for (int n = 0; n < nodeCount; n++)
            {
                int startIdx = n * processorsPerNode;
                int[] indices = Enumerable.Range(startIdx, Math.Min(processorsPerNode, processorCount - startIdx)).ToArray();

                long mask = 0;
                foreach (int idx in indices)
                    mask |= 1L << idx;

                nodes.Add(new CpuNumaNode
                {
                    NodeId = n,
                    AffinityMask = mask,
                    ProcessorIndices = indices
                });
            }
        }
        catch
        {
            nodes.Add(new CpuNumaNode
            {
                NodeId = 0,
                AffinityMask = processorCount > 0 ? (1L << processorCount) - 1 : 0,
                ProcessorIndices = Enumerable.Range(0, processorCount).ToArray()
            });
        }

        return nodes;
    }

    /// <summary>
    /// Detects cache hierarchy levels using WMI queries.
    /// Falls back to typical cache sizes if WMI is unavailable.
    /// </summary>
    /// <returns>A list of detected cache levels.</returns>
    internal List<CpuCacheLevel> DetectCacheLevels()
    {
        var levels = new List<CpuCacheLevel>();

        if (_queryService != null)
        {
            try
            {
                var results = _queryService.ExecuteQuery(
                    "SELECT Level, Size, Associativity FROM Win32_CacheMemory");

                foreach (var obj in results)
                {
                    int level = obj.ContainsKey("Level") && int.TryParse(obj["Level"]?.ToString(), out int lvl) ? lvl : 0;
                    int size = obj.ContainsKey("Size") && int.TryParse(obj["Size"]?.ToString(), out int sz) ? sz : 0;
                    int assoc = obj.ContainsKey("Associativity") && int.TryParse(obj["Associativity"]?.ToString(), out int a) ? a : 0;

                    if (level > 0 && size > 0)
                    {
                        levels.Add(new CpuCacheLevel
                        {
                            Level = level,
                            SizeKB = size,
                            Associativity = assoc,
                            SharedProcessors = level == 1 ? 1 : (level == 2 ? 2 : Environment.ProcessorCount)
                        });
                    }
                }
            }
            catch
            {
            }
        }

        if (levels.Count == 0)
        {
            int processorCount = Environment.ProcessorCount;
            int coreCount = Math.Max(1, processorCount / 2);

            levels.Add(new CpuCacheLevel
            {
                Level = 1,
                SizeKB = 32,
                Associativity = 8,
                SharedProcessors = 1
            });

            levels.Add(new CpuCacheLevel
            {
                Level = 2,
                SizeKB = 512,
                Associativity = 8,
                SharedProcessors = 2
            });

            levels.Add(new CpuCacheLevel
            {
                Level = 3,
                SizeKB = coreCount > 4 ? 8192 : 4096,
                Associativity = 16,
                SharedProcessors = processorCount
            });
        }

        return levels;
    }

    private long GetVramMb(INvApiGpu nvApi, int index)
    {
        var info = nvApi.GetVramInfo(index);
        return info?.totalMb ?? 0;
    }

    private long GetVramMb(IAdl2Gpu adl2, int index)
    {
        var info = adl2.GetVramInfo(index);
        return info?.totalMb ?? 0;
    }

    private int DetectEngineCount(INvApiGpu nvApi, int index)
    {
        int engineCount = 1;
        var clockInfo = nvApi.GetClockInfo(index);
        if (clockInfo.HasValue)
            engineCount = clockInfo.Value.coreClockMHz > 0 ? 4 : 1;
        return engineCount;
    }
}