namespace ZTR.Desktop.Features.Process.Services;

public interface IProcessManagerService
{
    bool SetProcessAffinity(int processId, long affinityMask);
    bool SetProcessPriority(int processId, int priority);
    long GetProcessAffinity(int processId);
    int GetProcessPriority(int processId);
}
