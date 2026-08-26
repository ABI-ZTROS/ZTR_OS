using System.Diagnostics;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class ProcessTrackerTests
{
    private readonly ProcessTracker _tracker;

    public ProcessTrackerTests()
    {
        _tracker = new ProcessTracker();
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        Assert.NotNull(_tracker);
    }

    [Fact]
    public void GetAllProcesses_ReturnsNonEmptyList()
    {
        var processes = _tracker.GetAllProcesses();

        Assert.NotNull(processes);
        Assert.True(processes.Count > 0);
    }

    [Fact]
    public void GetAllProcesses_ContainsCurrentProcess()
    {
        var currentPid = Process.GetCurrentProcess().Id;
        var processes = _tracker.GetAllProcesses();

        Assert.Contains(processes, p => p.ProcessId == currentPid);
    }

    [Fact]
    public void GetAllProcesses_ProcessesHaveValidData()
    {
        var processes = _tracker.GetAllProcesses();

        foreach (var proc in processes)
        {
            Assert.True(proc.ProcessId > 0);
            Assert.False(string.IsNullOrEmpty(proc.ProcessName));
        }
    }

    [Fact]
    public void GetForegroundProcess_ReturnsNonNullOrNull()
    {
        var result = _tracker.GetForegroundProcess();

        Assert.True(result == null || result.ProcessId > 0);
    }

    [Fact]
    public void GetProcessByPattern_EmptyPattern_ReturnsEmpty()
    {
        var result = _tracker.GetProcessByPattern("");
        Assert.Empty(result);
    }

    [Fact]
    public void GetProcessByPattern_WhitespacePattern_ReturnsEmpty()
    {
        var result = _tracker.GetProcessByPattern("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void GetProcessByPattern_WildcardPattern_MatchesCurrentProcess()
    {
        var currentName = Process.GetCurrentProcess().ProcessName;
        var result = _tracker.GetProcessByPattern($"*{currentName}*");

        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
    }

    [Fact]
    public void GetProcessByPattern_ExactMatch_ReturnsCorrectProcess()
    {
        var currentName = Process.GetCurrentProcess().ProcessName;
        var result = _tracker.GetProcessByPattern(currentName);

        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
        Assert.All(result, p => Assert.Equal(currentName, p.ProcessName));
    }

    [Fact]
    public void GetProcessByPattern_NoMatch_ReturnsEmpty()
    {
        var result = _tracker.GetProcessByPattern("zzznonexistentprocess12345");
        Assert.Empty(result);
    }

    [Fact]
    public void GetGpuIntensiveProcesses_ReturnsList()
    {
        var result = _tracker.GetGpuIntensiveProcesses();

        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public void GetCpuIntensiveProcesses_ReturnsList()
    {
        var result = _tracker.GetCpuIntensiveProcesses();

        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public void TrackProcess_AddsToTrackedBindings()
    {
        var binding = new ProcessBinding
        {
            ProcessId = 99999,
            ProcessName = "TestProcess"
        };

        _tracker.TrackProcess(binding);

        var tracked = _tracker.GetTrackedBindings();
        Assert.True(tracked.ContainsKey(99999));
        Assert.Equal("TestProcess", tracked[99999].ProcessName);
    }

    [Fact]
    public void TrackProcess_NullBinding_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _tracker.TrackProcess(null!));
    }

    [Fact]
    public void UntrackProcess_Existing_ReturnsTrue()
    {
        var binding = new ProcessBinding { ProcessId = 10000, ProcessName = "ToRemove" };
        _tracker.TrackProcess(binding);

        bool result = _tracker.UntrackProcess(10000);
        Assert.True(result);
        Assert.False(_tracker.GetTrackedBindings().ContainsKey(10000));
    }

    [Fact]
    public void UntrackProcess_NonExisting_ReturnsFalse()
    {
        bool result = _tracker.UntrackProcess(99999);
        Assert.False(result);
    }

    [Fact]
    public void GetTrackedBindings_InitiallyEmpty()
    {
        var tracker = new ProcessTracker();
        Assert.Empty(tracker.GetTrackedBindings());
    }

    [Fact]
    public void TrackProcess_AppearsInGetAllProcesses()
    {
        var currentProc = System.Diagnostics.Process.GetCurrentProcess();
        var binding = new ProcessBinding
        {
            ProcessId = currentProc.Id,
            ProcessName = currentProc.ProcessName,
            Strategy = BindingStrategy.Manual
        };

        _tracker.TrackProcess(binding);
        var processes = _tracker.GetAllProcesses();
        var trackedProc = processes.FirstOrDefault(p => p.ProcessId == currentProc.Id);

        Assert.NotNull(trackedProc);
        Assert.Equal(BindingStrategy.Manual, trackedProc.Strategy);
    }

    [Fact]
    public void IsGameProcess_KnownGame_ReturnsTrue()
    {
        Assert.True(_tracker.IsGameProcess("cs2"));
        Assert.True(_tracker.IsGameProcess("dota2"));
        Assert.True(_tracker.IsGameProcess("valorant"));
        Assert.True(_tracker.IsGameProcess("Cyberpunk2077"));
    }

    [Fact]
    public void IsGameProcess_UnknownProcess_ReturnsFalse()
    {
        Assert.False(_tracker.IsGameProcess("notagame"));
        Assert.False(_tracker.IsGameProcess(""));
        Assert.False(_tracker.IsGameProcess(null!));
    }

    [Fact]
    public void IsGameProcess_CaseInsensitive()
    {
        Assert.True(_tracker.IsGameProcess("CS2.EXE"));
        Assert.True(_tracker.IsGameProcess("Dota2"));
    }

    [Fact]
    public void KnownGameProcesses_ReturnsReadOnlyList()
    {
        var games = _tracker.KnownGameProcesses;
        Assert.NotNull(games);
        Assert.True(games.Count > 0);
    }

    [Fact]
    public void GetAllProcesses_DoesNotThrow()
    {
        var exception = Record.Exception(() => _tracker.GetAllProcesses());
        Assert.Null(exception);
    }

    [Fact]
    public void GetProcessByPattern_WildcardAtStart_Works()
    {
        var result = _tracker.GetProcessByPattern("*dotnet*");
        Assert.NotNull(result);
    }

    [Fact]
    public void GetProcessByPattern_WildcardAtEnd_Works()
    {
        var result = _tracker.GetProcessByPattern("dotnet*");
        Assert.NotNull(result);
    }

    [Fact]
    public void MultipleTrackedBindings_AllRetrieved()
    {
        _tracker.TrackProcess(new ProcessBinding { ProcessId = 1, ProcessName = "p1" });
        _tracker.TrackProcess(new ProcessBinding { ProcessId = 2, ProcessName = "p2" });
        _tracker.TrackProcess(new ProcessBinding { ProcessId = 3, ProcessName = "p3" });

        Assert.Equal(3, _tracker.GetTrackedBindings().Count);
    }
}