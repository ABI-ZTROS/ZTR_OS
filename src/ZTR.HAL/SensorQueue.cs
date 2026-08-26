using System.Collections.Concurrent;
using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Thread-safe producer-consumer queue for <see cref="HardwareState"/> sensor data.
/// Implements a ring buffer with configurable capacity for bounded memory usage.
/// Supports SignalR push and MLP consumption patterns.
/// </summary>
public class SensorQueue : IDisposable
{
    private readonly int _capacity;
    private readonly ConcurrentQueue<HardwareState> _queue;
    private readonly LinkedList<HardwareState> _history;
    private readonly object _syncRoot = new();
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Gets the current number of items in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_syncRoot) return _count;
        }
    }

    /// <summary>
    /// Gets the maximum capacity of the ring buffer.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets a value indicating whether the queue is empty.
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Creates a new instance of the <see cref="SensorQueue"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of <see cref="HardwareState"/> entries to retain.
    /// Defaults to 1000. Values less than 16 are clamped to 16.
    /// </param>
    public SensorQueue(int capacity = 1000)
    {
        _capacity = Math.Max(16, capacity);
        _queue = new ConcurrentQueue<HardwareState>();
        _history = new LinkedList<HardwareState>();
    }

    /// <summary>
    /// Adds a new <see cref="HardwareState"/> reading to the queue.
    /// When the queue reaches capacity, the oldest entry is automatically evicted.
    /// </summary>
    /// <param name="state">The hardware state to enqueue.</param>
    public void Enqueue(HardwareState state)
    {
        if (_disposed) return;

        ArgumentNullException.ThrowIfNull(state);

        lock (_syncRoot)
        {
            _queue.Enqueue(state);
            _history.AddLast(state);
            _count++;

            while (_count > _capacity && _history.First != null)
            {
                _history.RemoveFirst();
                _queue.TryDequeue(out _);
                _count--;
            }
        }

        RaiseStateEnqueued(state);
    }

    /// <summary>
    /// Attempts to dequeue the latest <see cref="HardwareState"/> reading.
    /// </summary>
    /// <param name="state">
    /// When this method returns, contains the dequeued state if successful;
    /// otherwise null.
    /// </param>
    /// <returns>True if a state was successfully dequeued; otherwise false.</returns>
    public bool TryDequeue(out HardwareState? state)
    {
        state = null;
        if (_disposed) return false;

        lock (_syncRoot)
        {
            if (_queue.TryDequeue(out state))
            {
                _count--;
                if (_history.First != null && ReferenceEquals(_history.First.Value, state))
                {
                    _history.RemoveFirst();
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to peek at the latest <see cref="HardwareState"/> without removing it.
    /// </summary>
    /// <param name="state">
    /// When this method returns, contains the peeked state if successful;
    /// otherwise null.
    /// </param>
    /// <returns>True if a state was successfully peeked; otherwise false.</returns>
    public bool TryPeek(out HardwareState? state)
    {
        state = null;
        if (_disposed) return false;
        return _queue.TryPeek(out state);
    }

    /// <summary>
    /// Gets the most recent <see cref="HardwareState"/> readings up to the specified count.
    /// </summary>
    /// <param name="count">The number of recent readings to retrieve.</param>
    /// <returns>A read-only list of the most recent readings, newest first.</returns>
    public IReadOnlyList<HardwareState> GetHistory(int count)
    {
        if (_disposed || count <= 0)
            return Array.Empty<HardwareState>();

        lock (_syncRoot)
        {
            var result = new List<HardwareState>(Math.Min(count, _history.Count));
            var node = _history.Last;
            while (node != null && result.Count < count)
            {
                result.Add(node.Value);
                node = node.Previous;
            }
            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all currently stored <see cref="HardwareState"/> readings.
    /// </summary>
    /// <returns>A read-only list of all readings, oldest first.</returns>
    public IReadOnlyList<HardwareState> GetAll()
    {
        if (_disposed)
            return Array.Empty<HardwareState>();

        lock (_syncRoot)
        {
            return _history.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all entries from the queue and history.
    /// </summary>
    public void Clear()
    {
        if (_disposed) return;

        lock (_syncRoot)
        {
            while (_queue.TryDequeue(out _))
            {
            }

            _history.Clear();
            _count = 0;
        }
    }

    /// <summary>
    /// Raised when a new <see cref="HardwareState"/> is enqueued.
    /// Subscribers receive the newly added state.
    /// </summary>
    public event EventHandler<HardwareState>? StateEnqueued;

    /// <summary>
    /// Raises the <see cref="StateEnqueued"/> event.
    /// </summary>
    /// <param name="state">The state that was enqueued.</param>
    internal void RaiseStateEnqueued(HardwareState state)
    {
        StateEnqueued?.Invoke(this, state);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Clear();
            StateEnqueued = null;
        }

        _disposed = true;
    }
}
