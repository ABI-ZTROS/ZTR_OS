using System.Runtime.InteropServices;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Manages CPU affinity for processes and threads using Win32 API.
/// Provides process-level, thread-level, and NUMA node-level affinity management.
/// </summary>
public class CpuAffinityManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessAffinityMask(IntPtr hProcess, long dwProcessAffinityMask);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessAffinityMask(IntPtr hProcess, out long lpProcessAffinityMask, out long lpSystemAffinityMask);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessorNumber();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetThreadAffinityMask(IntPtr hThread, long dwThreadAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetThreadAffinityMask(IntPtr hThread, out long lpThreadAffinityMask);
    
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_SET_INFORMATION = 0x0200;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint THREAD_QUERY_INFORMATION = 0x0040;
    private const uint THREAD_SET_INFORMATION = 0x0020;
    private const uint THREAD_QUERY_LIMITED_INFORMATION = 0x0400;
    
    /// <summary>
    /// Set CPU affinity mask for a process.
    /// On non-Windows platforms, gracefully falls back to returning false.
    /// </summary>
    public bool SetAffinity(int processId, long affinityMask)
    {
        try
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_INFORMATION, false, processId);
            if (handle == IntPtr.Zero) return false;

            try
            {
                return SetProcessAffinityMask(handle, affinityMask);
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Get current CPU affinity mask for a process.
    /// On non-Windows platforms, gracefully falls back to returning null.
    /// </summary>
    public (long processMask, long systemMask)? GetAffinity(int processId)
    {
        try
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == IntPtr.Zero) return null;

            try
            {
                if (GetProcessAffinityMask(handle, out long processMask, out long systemMask))
                    return (processMask, systemMask);
                return null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Create affinity mask from core indices.
    /// </summary>
    public static long CreateMask(params int[] coreIndices)
    {
        long mask = 0;
        foreach (int idx in coreIndices)
        {
            mask |= 1L << idx;
        }
        return mask;
    }
    
    /// <summary>
    /// Get number of logical processors on the system.
    /// </summary>
    public static int GetLogicalProcessorCount() => Environment.ProcessorCount;
    
    /// <summary>
    /// Get current CPU topology.
    /// </summary>
    public CpuTopology GetTopology()
    {
        int processorCount = GetLogicalProcessorCount();
        int coreCount = processorCount / 2; // Assume hyperthreading
        
        var numaNodes = new List<CpuNumaNode>();
        // Simplified: assume single NUMA node for laptops
        numaNodes.Add(new CpuNumaNode
        {
            NodeId = 0,
            AffinityMask = CreateMask(Enumerable.Range(0, processorCount).ToArray()),
            ProcessorIndices = Enumerable.Range(0, processorCount).ToArray()
        });
        
        return new CpuTopology
        {
            TotalCores = coreCount,
            TotalLogicalProcessors = processorCount,
            NumaNodeCount = 1,
            NumaNodes = numaNodes
        };
    }
    
    /// <summary>
    /// List all running processes with their affinity.
    /// </summary>
    public IReadOnlyList<ProcessBinding> ListProcesses()
    {
        var result = new List<ProcessBinding>();
        var processes = System.Diagnostics.Process.GetProcesses();
        
        foreach (var proc in processes)
        {
            try
            {
                var affinity = GetAffinity(proc.Id);
                result.Add(new ProcessBinding
                {
                    ProcessId = proc.Id,
                    ProcessName = proc.ProcessName,
                    CpuAffinity = new CpuAffinityConfig
                    {
                        Enabled = affinity.HasValue,
                        AffinityMask = affinity?.processMask ?? 0
                    }
                });
            }
            catch
            {
            }
        }
        
        return result;
    }

    /// <summary>
    /// Gets the thread-level affinity mask for a specific thread.
    /// On non-Windows platforms, gracefully falls back to returning null.
    /// </summary>
    /// <param name="threadId">The thread identifier to query.</param>
    /// <returns>The thread affinity mask, or null if unable to retrieve.</returns>
    public long? GetThreadAffinityMask(int threadId)
    {
        if (threadId <= 0)
            return null;

        try
        {
            IntPtr handle = OpenThread(THREAD_QUERY_LIMITED_INFORMATION, false, (uint)threadId);
            if (handle == IntPtr.Zero)
                return null;

            try
            {
                if (GetThreadAffinityMask(handle, out long mask))
                    return mask;
                return null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the thread-level affinity mask for a specific thread.
    /// On non-Windows platforms, gracefully falls back to returning false.
    /// </summary>
    /// <param name="threadId">The thread identifier to modify.</param>
    /// <param name="mask">The affinity mask to apply to the thread.</param>
    /// <returns>True if the thread affinity was set successfully; otherwise false.</returns>
    public bool SetThreadAffinityMask(int threadId, long mask)
    {
        if (threadId <= 0 || mask <= 0)
            return false;

        try
        {
            IntPtr handle = OpenThread(THREAD_QUERY_INFORMATION | THREAD_SET_INFORMATION, false, (uint)threadId);
            if (handle == IntPtr.Zero)
                return false;

            try
            {
                return SetThreadAffinityMask(handle, mask);
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the CPU affinity mask for a specific NUMA node.
    /// </summary>
    /// <param name="nodeId">The NUMA node identifier.</param>
    /// <returns>The NUMA node's affinity mask, or 0 if the node is not found.</returns>
    public long GetNumaNodeAffinity(int nodeId)
    {
        var topology = GetTopology();
        var node = topology.NumaNodes.FirstOrDefault(n => n.NodeId == nodeId);
        return node?.AffinityMask ?? 0;
    }

    /// <summary>
    /// Binds a process to a specific NUMA node by setting its CPU affinity mask
    /// to include only the processors belonging to that node.
    /// </summary>
    /// <param name="processId">The process identifier to bind.</param>
    /// <param name="nodeId">The NUMA node identifier to bind to.</param>
    /// <returns>True if the process was successfully bound to the NUMA node; otherwise false.</returns>
    public bool SetProcessNumaNode(int processId, int nodeId)
    {
        if (processId <= 0)
            return false;

        long numaMask = GetNumaNodeAffinity(nodeId);
        if (numaMask == 0)
            return false;

        return SetAffinity(processId, numaMask);
    }

    /// <summary>
    /// Gets the list of processor indices belonging to a NUMA node.
    /// </summary>
    /// <param name="nodeId">The NUMA node identifier.</param>
    /// <returns>An array of processor indices, or an empty array if node not found.</returns>
    public int[] GetNumaNodeProcessors(int nodeId)
    {
        var topology = GetTopology();
        var node = topology.NumaNodes.FirstOrDefault(n => n.NodeId == nodeId);
        return node?.ProcessorIndices ?? Array.Empty<int>();
    }

    /// <summary>
    /// Creates an affinity mask for a NUMA node with optional core count limit.
    /// </summary>
    /// <param name="nodeId">The NUMA node identifier.</param>
    /// <param name="maxCores">The maximum number of cores to use, or 0 for all.</param>
    /// <returns>The affinity mask for the NUMA node.</returns>
    public long CreateNumaNodeMask(int nodeId, int maxCores = 0)
    {
        var processors = GetNumaNodeProcessors(nodeId);
        if (processors.Length == 0)
            return 0;

        if (maxCores > 0 && maxCores < processors.Length)
            processors = processors.Take(maxCores).ToArray();

        return CreateMask(processors);
    }
}