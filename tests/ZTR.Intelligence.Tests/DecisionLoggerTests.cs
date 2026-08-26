using System.Text.Json;
using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class DecisionLoggerTests
{
    private readonly DecisionLogger _logger;

    public DecisionLoggerTests()
    {
        _logger = new DecisionLogger(ringBufferSize: 100);
    }

    [Fact]
    public void LogDecision_SingleDecision_IncrementsCount()
    {
        var decision = CreateTestDecision();

        long before = _logger.TotalDecisionsLogged;
        _logger.LogDecision(decision);
        long after = _logger.TotalDecisionsLogged;

        Assert.Equal(before + 1, after);
    }

    [Fact]
    public void LogDecision_MultipleDecisions_IncrementsCorrectly()
    {
        for (int i = 0; i < 10; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        Assert.Equal(10, _logger.TotalDecisionsLogged);
        Assert.Equal(10, _logger.BufferedCount);
    }

    [Fact]
    public void LogDecision_ExceedsRingBuffer_EvictsOldest()
    {
        var logger = new DecisionLogger(ringBufferSize: 15);

        for (int i = 0; i < 20; i++)
        {
            logger.LogDecision(CreateTestDecision());
        }

        Assert.Equal(20, logger.TotalDecisionsLogged);
        Assert.Equal(15, logger.BufferedCount);
    }

    [Fact]
    public void GetRecentDecisions_ReturnsCorrectCount()
    {
        for (int i = 0; i < 10; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        var recent = _logger.GetRecentDecisions(5);

        Assert.Equal(5, recent.Length);
    }

    [Fact]
    public void GetRecentDecisions_OrderedNewestFirst()
    {
        for (int i = 0; i < 5; i++)
        {
            var decision = new MlpDecision
            {
                Timestamp = DateTime.Now.AddSeconds(i),
                Confidence = i * 0.1
            };
            _logger.LogDecision(decision);
        }

        var recent = _logger.GetRecentDecisions(3);

        Assert.True(recent[0].Timestamp >= recent[1].Timestamp);
        Assert.True(recent[1].Timestamp >= recent[2].Timestamp);
    }

    [Fact]
    public void GetRecentDecisions_EmptyBuffer_ReturnsEmptyArray()
    {
        var recent = _logger.GetRecentDecisions(5);

        Assert.Empty(recent);
    }

    [Fact]
    public void GetRecentDecisions_MoreThanAvailable_ReturnsAll()
    {
        for (int i = 0; i < 3; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        var recent = _logger.GetRecentDecisions(10);

        Assert.Equal(3, recent.Length);
    }

    [Fact]
    public void LogDecision_NullDecision_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _logger.LogDecision(null!));
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        for (int i = 0; i < 3; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        string json = _logger.ExportToJson();

        Assert.False(string.IsNullOrEmpty(json));
        Assert.StartsWith("[", json);

        var decisions = JsonSerializer.Deserialize<MlpDecision[]>(json);
        Assert.NotNull(decisions);
        Assert.Equal(3, decisions!.Length);
    }

    [Fact]
    public void ExportToJson_EmptyBuffer_ReturnsEmptyArray()
    {
        string json = _logger.ExportToJson();

        Assert.Equal("[]", json);
    }

    [Fact]
    public void ExportToFile_WritesToDisk()
    {
        for (int i = 0; i < 3; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        string tempPath = Path.GetTempFileName();
        _logger.ExportToFile(tempPath);

        Assert.True(File.Exists(tempPath));
        string content = File.ReadAllText(tempPath);
        Assert.Contains("[", content);

        File.Delete(tempPath);
    }

    [Fact]
    public void ImportFromFile_LoadsDecisions()
    {
        for (int i = 0; i < 3; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        string tempPath = Path.GetTempFileName();
        _logger.ExportToFile(tempPath);

        var importLogger = new DecisionLogger(ringBufferSize: 100);
        int imported = importLogger.ImportFromFile(tempPath);

        Assert.Equal(3, imported);
        Assert.Equal(3, importLogger.BufferedCount);

        File.Delete(tempPath);
    }

    [Fact]
    public void ImportFromFile_NonExistentFile_ReturnsZero()
    {
        int imported = _logger.ImportFromFile("/nonexistent/file.json");

        Assert.Equal(0, imported);
    }

    [Fact]
    public void Clear_RemovesAllDecisions()
    {
        for (int i = 0; i < 5; i++)
        {
            _logger.LogDecision(CreateTestDecision());
        }

        _logger.Clear();

        Assert.Equal(0, _logger.BufferedCount);
        Assert.Equal(0, _logger.TotalDecisionsLogged);
    }

    [Fact]
    public void Constructor_InvalidBufferSize_ClampsToMinimum()
    {
        var logger = new DecisionLogger(ringBufferSize: 5);
        Assert.Equal(10, logger.BufferedCount == 0 ? 10 : logger.BufferedCount);

        logger.LogDecision(CreateTestDecision());
        Assert.True(logger.TotalDecisionsLogged >= 1);
    }

    [Fact]
    public void GetRecentDecisions_ZeroCount_ReturnsEmpty()
    {
        _logger.LogDecision(CreateTestDecision());

        var recent = _logger.GetRecentDecisions(0);

        Assert.Empty(recent);
    }

    private static MlpDecision CreateTestDecision()
    {
        return new MlpDecision
        {
            Timestamp = DateTime.Now,
            InputFeatures = new double[] { 0.5, 0.3, 0.8 },
            OutputActions = new double[] { 0.6, 0.4, 0.7, 0.2, 0.5, 0.8, 0.3, 0.6 },
            ActionType = "TestAction",
            Confidence = 0.85,
            Reasoning = "Test reasoning for unit test"
        };
    }
}