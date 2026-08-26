using System.Collections.Concurrent;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class SensorQueueTests : IDisposable
{
    private readonly SensorQueue _queue;
    private bool _disposed;

    public SensorQueueTests()
    {
        _queue = new SensorQueue(100);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _queue.Dispose();
        }
    }

    #region Enqueue Tests

    [Fact]
    public void Enqueue_SingleItem_IncrementsCount()
    {
        var state = CreateTestState();
        _queue.Enqueue(state);

        Assert.Equal(1, _queue.Count);
        Assert.False(_queue.IsEmpty);
    }

    [Fact]
    public void Enqueue_MultipleItems_IncrementsCount()
    {
        for (int i = 0; i < 10; i++)
        {
            _queue.Enqueue(CreateTestState());
        }

        Assert.Equal(10, _queue.Count);
    }

    [Fact]
    public void Enqueue_AtCapacity_EvictsOldest()
    {
        var smallQueue = new SensorQueue(16);
        var items = new List<HardwareState>();
        for (int i = 0; i < 20; i++)
        {
            items.Add(CreateTestState(timestamp: DateTime.UtcNow.AddSeconds(-20 + i)));
        }

        foreach (var item in items)
        {
            smallQueue.Enqueue(item);
        }

        Assert.Equal(16, smallQueue.Count);
        var history = smallQueue.GetHistory(16);
        Assert.Equal(items[19].Timestamp, history[0].Timestamp);
        Assert.Equal(items[18].Timestamp, history[1].Timestamp);
    }

    [Fact]
    public void Enqueue_NullState_DoesNotThrow()
    {
        var queue = new SensorQueue(10);
        Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null!));
    }

    #endregion

    #region TryDequeue Tests

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var result = _queue.TryDequeue(out var state);

        Assert.False(result);
        Assert.Null(state);
    }

    [Fact]
    public void TryDequeue_WithItems_ReturnsState()
    {
        var state = CreateTestState();
        _queue.Enqueue(state);

        var result = _queue.TryDequeue(out var dequeued);

        Assert.True(result);
        Assert.NotNull(dequeued);
        Assert.Equal(state.Timestamp, dequeued!.Timestamp);
    }

    [Fact]
    public void TryDequeue_DecreasesCount()
    {
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());

        _queue.TryDequeue(out _);

        Assert.Equal(1, _queue.Count);
    }

    [Fact]
    public void TryDequeue_AllItems_QueueBecomesEmpty()
    {
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());

        _queue.TryDequeue(out _);
        _queue.TryDequeue(out _);
        var result = _queue.TryDequeue(out _);

        Assert.False(result);
        Assert.True(_queue.IsEmpty);
    }

    #endregion

    #region TryPeek Tests

    [Fact]
    public void TryPeek_EmptyQueue_ReturnsFalse()
    {
        var result = _queue.TryPeek(out var state);

        Assert.False(result);
        Assert.Null(state);
    }

    [Fact]
    public void TryPeek_WithItems_DoesNotRemove()
    {
        _queue.Enqueue(CreateTestState());

        var result = _queue.TryPeek(out var state);

        Assert.True(result);
        Assert.NotNull(state);
        Assert.Equal(1, _queue.Count);
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public void GetHistory_EmptyQueue_ReturnsEmpty()
    {
        var history = _queue.GetHistory(10);

        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_FewerItemsThanRequested_ReturnsAll()
    {
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());

        var history = _queue.GetHistory(10);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void GetHistory_MoreItemsThanRequested_ReturnsRequestedCount()
    {
        for (int i = 0; i < 10; i++)
        {
            _queue.Enqueue(CreateTestState());
        }

        var history = _queue.GetHistory(5);

        Assert.Equal(5, history.Count);
    }

    [Fact]
    public void GetHistory_ZeroCount_ReturnsEmpty()
    {
        _queue.Enqueue(CreateTestState());

        var history = _queue.GetHistory(0);

        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_NegativeCount_ReturnsEmpty()
    {
        _queue.Enqueue(CreateTestState());

        var history = _queue.GetHistory(-5);

        Assert.Empty(history);
    }

    [Fact]
    public void GetHistory_OrderIsNewestFirst()
    {
        var older = CreateTestState(timestamp: DateTime.UtcNow.AddSeconds(-10));
        var newer = CreateTestState(timestamp: DateTime.UtcNow);

        _queue.Enqueue(older);
        _queue.Enqueue(newer);

        var history = _queue.GetHistory(2);

        Assert.Equal(newer.Timestamp, history[0].Timestamp);
        Assert.Equal(older.Timestamp, history[1].Timestamp);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_EmptyQueue_ReturnsEmpty()
    {
        var all = _queue.GetAll();

        Assert.Empty(all);
    }

    [Fact]
    public void GetAll_WithItems_ReturnsAll()
    {
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());

        var all = _queue.GetAll();

        Assert.Equal(3, all.Count);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_EmptyQueue_DoesNotThrow()
    {
        _queue.Clear();
        Assert.True(_queue.IsEmpty);
    }

    [Fact]
    public void Clear_WithItems_RemovesAll()
    {
        for (int i = 0; i < 5; i++)
        {
            _queue.Enqueue(CreateTestState());
        }

        _queue.Clear();

        Assert.True(_queue.IsEmpty);
        Assert.Equal(0, _queue.Count);
    }

    [Fact]
    public void Clear_AfterEnqueue_CountIsZero()
    {
        _queue.Enqueue(CreateTestState());
        _queue.Enqueue(CreateTestState());
        _queue.Clear();
        _queue.Enqueue(CreateTestState());

        Assert.Equal(1, _queue.Count);
    }

    #endregion

    #region Capacity Tests

    [Fact]
    public void Capacity_Default_Is1000()
    {
        var queue = new SensorQueue();
        Assert.Equal(1000, queue.Capacity);
    }

    [Fact]
    public void Capacity_Custom_SetCorrectly()
    {
        var queue = new SensorQueue(50);
        Assert.Equal(50, queue.Capacity);
    }

    [Fact]
    public void Capacity_Below16_ClampedTo16()
    {
        var queue = new SensorQueue(5);
        Assert.Equal(16, queue.Capacity);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public void ConcurrentEnqueue_ManyThreads_NoExceptions()
    {
        var queue = new SensorQueue(1000);
        int threadCount = 10;
        int itemsPerThread = 100;
        var exceptions = new List<Exception>();

        Parallel.For(0, threadCount, threadId =>
        {
            try
            {
                for (int i = 0; i < itemsPerThread; i++)
                {
                    queue.Enqueue(CreateTestState());
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(threadCount * itemsPerThread, queue.Count);
    }

    [Fact]
    public void ConcurrentEnqueueAndDequeue_NoExceptions()
    {
        var queue = new SensorQueue(1000);
        int producers = 5;
        int consumers = 5;
        int itemsPerProducer = 100;
        var exceptions = new List<Exception>();
        var consumed = new ConcurrentBag<HardwareState>();

        Parallel.For(0, producers, i =>
        {
            try
            {
                for (int j = 0; j < itemsPerProducer; j++)
                {
                    queue.Enqueue(CreateTestState());
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Parallel.For(0, consumers, i =>
        {
            try
            {
                while (queue.TryDequeue(out var state))
                {
                    if (state != null)
                        consumed.Add(state);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.True(consumed.Count >= producers * itemsPerProducer - queue.Count);
    }

    [Fact]
    public void ConcurrentEnqueueAndHistory_NoExceptions()
    {
        var queue = new SensorQueue(1000);
        var exceptions = new List<Exception>();

        Parallel.For(0, 10, i =>
        {
            try
            {
                for (int j = 0; j < 50; j++)
                {
                    queue.Enqueue(CreateTestState());
                    queue.GetHistory(10);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentMixedOperations_NoDeadlocks()
    {
        var queue = new SensorQueue(200);
        var exceptions = new List<Exception>();
        var consumed = new ConcurrentBag<HardwareState>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(CreateTestState());
        }

        Parallel.For(0, 8, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    while (queue.TryDequeue(out var state))
                    {
                        if (state != null) consumed.Add(state);
                    }
                }
                else
                {
                    queue.GetHistory(10);
                    queue.GetAll();
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ReleasesResources()
    {
        var queue = new SensorQueue();
        queue.Enqueue(CreateTestState());
        queue.Dispose();

        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        var queue = new SensorQueue();
        queue.Dispose();
        queue.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void Enqueue_AfterDispose_DoesNotThrow()
    {
        var queue = new SensorQueue();
        queue.Dispose();
        queue.Enqueue(CreateTestState());
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TryDequeue_AfterDispose_ReturnsFalse()
    {
        var queue = new SensorQueue();
        queue.Dispose();

        var result = queue.TryDequeue(out _);
        Assert.False(result);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void StateEnqueued_Event_FiredOnEnqueue()
    {
        var queue = new SensorQueue();
        HardwareState? captured = null;
        queue.StateEnqueued += (_, state) => captured = state;

        queue.Enqueue(CreateTestState());

        Assert.NotNull(captured);
    }

    #endregion

    #region Helper

    private static HardwareState CreateTestState(DateTime? timestamp = null)
    {
        return new HardwareState
        {
            Timestamp = timestamp ?? DateTime.UtcNow,
            Cpu = new CpuState { Temperature = 45, Usage = 30, Power = 65, ClockMHz = 3200 },
            Gpu = new GpuState { Temperature = 60, Usage = 75, Power = 180, CoreClockMHz = 1800, MemoryClockMHz = 5000, UsedVramMB = 6144, TotalVramMB = 8192 },
            Battery = new BatteryState { ChargePercent = 75, IsCharging = true, ChargeLimit = 80 },
            Fan = new FanState { CpuFanRpm = 2400, GpuFanRpm = 3200, CpuFanSpeed = 50, GpuFanSpeed = 65 }
        };
    }

    #endregion
}
