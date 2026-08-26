using System.Text.Json;
using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// Logs every MLP decision including input features, output actions,
/// confidence, and reasoning. Stores recent decisions in a thread-safe
/// ring buffer and supports JSON export for analysis.
/// </summary>
public class DecisionLogger
{
    private readonly int _ringBufferSize;
    private readonly List<MlpDecision> _recentDecisions;
    private readonly object _lock = new();
    private long _totalDecisionsLogged;

    /// <summary>
    /// Gets the total number of decisions logged since creation.
    /// </summary>
    public long TotalDecisionsLogged => Interlocked.Read(ref _totalDecisionsLogged);

    /// <summary>
    /// Gets the number of decisions currently stored in the ring buffer.
    /// </summary>
    public int BufferedCount
    {
        get { lock (_lock) { return _recentDecisions.Count; } }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DecisionLogger"/> class.
    /// </summary>
    /// <param name="ringBufferSize">Maximum number of recent decisions to retain.</param>
    public DecisionLogger(int ringBufferSize = 1000)
    {
        _ringBufferSize = Math.Max(ringBufferSize, 10);
        _recentDecisions = new List<MlpDecision>(_ringBufferSize);
        _totalDecisionsLogged = 0;
    }

    /// <summary>
    /// Logs an MLP decision to the ring buffer.
    /// </summary>
    /// <param name="decision">The MLP decision to log.</param>
    public void LogDecision(MlpDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        lock (_lock)
        {
            _recentDecisions.Add(decision);

            while (_recentDecisions.Count > _ringBufferSize)
            {
                _recentDecisions.RemoveAt(0);
            }
        }

        Interlocked.Increment(ref _totalDecisionsLogged);
    }

    /// <summary>
    /// Retrieves the most recent decisions from the ring buffer.
    /// </summary>
    /// <param name="count">Maximum number of recent decisions to retrieve.</param>
    /// <returns>An array of recent MLP decisions, ordered from most to least recent.</returns>
    public MlpDecision[] GetRecentDecisions(int count)
    {
        count = Math.Min(count, _ringBufferSize);

        lock (_lock)
        {
            count = Math.Min(count, _recentDecisions.Count);

            if (count <= 0)
                return Array.Empty<MlpDecision>();

            var result = new MlpDecision[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = _recentDecisions[_recentDecisions.Count - 1 - i];
            }

            return result;
        }
    }

    /// <summary>
    /// Exports all ring buffer decisions to a JSON string.
    /// </summary>
    /// <returns>A JSON string containing all logged decisions.</returns>
    public string ExportToJson()
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(_recentDecisions, new JsonSerializerOptions { WriteIndented = true });
        }
        return json;
    }

    /// <summary>
    /// Exports all ring buffer decisions to a file in JSON format.
    /// </summary>
    /// <param name="filePath">Path to the output file.</param>
    public void ExportToFile(string filePath)
    {
        string json = ExportToJson();
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads decisions from a JSON file and adds them to the ring buffer.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <returns>The number of decisions loaded.</returns>
    public int ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;

        string json = File.ReadAllText(filePath);
        var decisions = JsonSerializer.Deserialize<List<MlpDecision>>(json);

        if (decisions == null || decisions.Count == 0)
            return 0;

        lock (_lock)
        {
            foreach (var decision in decisions)
            {
                _recentDecisions.Add(decision);
                Interlocked.Increment(ref _totalDecisionsLogged);
            }

            while (_recentDecisions.Count > _ringBufferSize)
            {
                _recentDecisions.RemoveAt(0);
            }
        }

        return decisions.Count;
    }

    /// <summary>
    /// Clears all decisions from the ring buffer and resets the counter.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _recentDecisions.Clear();
        }

        Interlocked.Exchange(ref _totalDecisionsLogged, 0);
    }
}