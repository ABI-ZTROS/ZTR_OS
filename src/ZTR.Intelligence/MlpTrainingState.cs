using System.Threading;
using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// V5 FIXED: Singleton training state service that persists training state across requests.
/// Previously _isTraining was a scoped instance field → every request reset it → status always "idle".
/// </summary>
public class MlpTrainingState
{
    private readonly object _lock = new();
    private CancellationTokenSource? _trainingCts;

    public bool IsTraining { get; private set; }
    public int CurrentEpoch { get; private set; }
    public double Loss { get; private set; }
    public int TotalSamplesTrained { get; private set; }

    public event EventHandler<MlpTrainingEventArgs>? TrainingProgress;
    public event EventHandler? TrainingCompleted;

    public void Start()
    {
        lock (_lock)
        {
            _trainingCts = new CancellationTokenSource();
            IsTraining = true;
            CurrentEpoch = 0;
            Loss = 0;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _trainingCts?.Cancel();
            IsTraining = false;
        }
    }

    public void UpdateProgress(int epoch, double loss)
    {
        lock (_lock)
        {
            CurrentEpoch = epoch;
            Loss = loss;
            TrainingProgress?.Invoke(this, new MlpTrainingEventArgs(epoch, loss));
        }
    }

    public void Complete()
    {
        lock (_lock)
        {
            IsTraining = false;
            _trainingCts?.Dispose();
            _trainingCts = null;
        }
        TrainingCompleted?.Invoke(this, EventArgs.Empty);
    }

    public CancellationToken GetCancellationToken()
    {
        lock (_lock)
        {
            return _trainingCts?.Token ?? CancellationToken.None;
        }
    }

    public void IncrementSamples(int count)
    {
        lock (_lock)
        {
            TotalSamplesTrained += count;
        }
    }
}

public class MlpTrainingEventArgs : EventArgs
{
    public int Epoch { get; }
    public double Loss { get; }

    public MlpTrainingEventArgs(int epoch, double loss)
    {
        Epoch = epoch;
        Loss = loss;
    }
}
