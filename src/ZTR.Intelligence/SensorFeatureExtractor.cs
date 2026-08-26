using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// Extracts temporal and statistical features from hardware sensor data
/// to produce a normalized feature vector for the MLP neural network.
/// </summary>
public class SensorFeatureExtractor
{
    private readonly int _windowSeconds;
    private readonly int _featureCount;

    /// <summary>
    /// Gets the number of features in the output vector.
    /// </summary>
    public int FeatureCount => _featureCount;

    /// <summary>
    /// Gets the sliding window size in seconds.
    /// </summary>
    public int WindowSeconds => _windowSeconds;

    /// <summary>
    /// Creates a new instance of the <see cref="SensorFeatureExtractor"/> class.
    /// </summary>
    /// <param name="windowSeconds">Sliding window duration for statistical features in seconds.</param>
    /// <param name="featureCount">Number of output features (default 16).</param>
    public SensorFeatureExtractor(int windowSeconds = 5, int featureCount = 16)
    {
        _windowSeconds = Math.Clamp(windowSeconds, 1, 60);
        _featureCount = featureCount;
    }

    /// <summary>
    /// Extracts a normalized feature vector from the current hardware state and its history.
    /// </summary>
    /// <param name="current">The current hardware state.</param>
    /// <param name="history">Recent historical hardware states ordered oldest-first.</param>
    /// <returns>A normalized double array of feature values in the range [0, 1].</returns>
    public double[] ExtractFeatures(HardwareState current, HardwareState[] history)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(history);

        var features = new double[_featureCount];

        if (_featureCount >= 1)
            features[0] = NormalizeTemperature(current.Cpu.Temperature);
        if (_featureCount >= 2)
            features[1] = NormalizeUsage(current.Cpu.Usage);
        if (_featureCount >= 3)
            features[2] = NormalizePower(current.Cpu.Power);
        if (_featureCount >= 4)
            features[3] = NormalizeClock(current.Cpu.ClockMHz);
        if (_featureCount >= 5)
            features[4] = NormalizeTemperature(current.Gpu.Temperature);
        if (_featureCount >= 6)
            features[5] = NormalizeTemperature(current.Gpu.HotspotTemperature);
        if (_featureCount >= 7)
            features[6] = NormalizeUsage(current.Gpu.Usage);
        if (_featureCount >= 8)
            features[7] = NormalizePower(current.Gpu.Power);
        if (_featureCount >= 9)
            features[8] = NormalizeVramUsage(current.Gpu);
        if (_featureCount >= 10)
            features[9] = NormalizeClock(current.Gpu.CoreClockMHz);
        if (_featureCount >= 11)
            features[10] = NormalizeCharge(current.Battery.ChargePercent);
        if (_featureCount >= 12)
            features[11] = current.Battery.IsCharging ? 1.0 : 0.0;
        if (_featureCount >= 13)
            features[12] = NormalizeFanSpeed(current.Fan.CpuFanSpeed);
        if (_featureCount >= 14)
            features[13] = NormalizeFanSpeed(current.Fan.GpuFanSpeed);

        if (_featureCount >= 15)
        {
            double cpuTempDeriv = ComputeDerivative(history, current, s => (double)s.Cpu.Temperature);
            features[14] = Math.Clamp(cpuTempDeriv * 0.5 + 0.5, 0.0, 1.0);
        }

        if (_featureCount >= 16)
        {
            double cpuUsageDeriv = ComputeDerivative(history, current, s => (double)s.Cpu.Usage);
            features[15] = Math.Clamp(cpuUsageDeriv * 0.5 + 0.5, 0.0, 1.0);
        }

        if (_featureCount >= 17)
        {
            double gpuTempDeriv = ComputeDerivative(history, current, s => (double)s.Gpu.Temperature);
            features[16] = Math.Clamp(gpuTempDeriv * 0.5 + 0.5, 0.0, 1.0);
        }

        if (_featureCount >= 18)
        {
            double gpuUsageDeriv = ComputeDerivative(history, current, s => (double)s.Gpu.Usage);
            features[17] = Math.Clamp(gpuUsageDeriv * 0.5 + 0.5, 0.0, 1.0);
        }

        if (_featureCount >= 19)
        {
            features[18] = ComputeLoadState(current);
        }

        if (_featureCount >= 20)
        {
            features[19] = ComputeEfficiencyRatio(current);
        }

        return features;
    }

    /// <summary>
    /// Computes a rate of change (derivative) for a given metric across the history window.
    /// </summary>
    /// <param name="history">Historical states.</param>
    /// <param name="current">Current state.</param>
    /// <param name="selector">Function to extract the metric value.</param>
    /// <returns>The normalized derivative value clamped to [-1, 1] range.</returns>
    private static double ComputeDerivative(HardwareState[] history, HardwareState current, Func<HardwareState, double> selector)
    {
        if (history.Length == 0)
            return 0.0;

        var oldest = history[0];
        double oldestVal = selector(oldest);
        double currentVal = selector(current);

        double timeSpan = (current.Timestamp - oldest.Timestamp).TotalSeconds;
        if (timeSpan <= 0)
            return 0.0;

        double rawDerivative = (currentVal - oldestVal) / timeSpan;

        double maxVal = Math.Max(Math.Abs(currentVal), Math.Abs(oldestVal));
        if (maxVal < 1.0)
            maxVal = 1.0;

        return Math.Clamp(rawDerivative / maxVal, -1.0, 1.0);
    }

    /// <summary>
    /// Classifies the current load state as a normalized value: idle (0.125), light (0.375), medium (0.625), heavy (0.875).
    /// </summary>
    /// <param name="state">Current hardware state.</param>
    /// <returns>A normalized value representing the load state.</returns>
    private static double ComputeLoadState(HardwareState state)
    {
        double cpuUsage = state.Cpu.Usage / 100.0;
        double gpuUsage = state.Gpu.Usage / 100.0;
        double avg = (cpuUsage + gpuUsage) / 2.0;

        return avg switch
        {
            < 0.15 => 0.125,
            < 0.4 => 0.375,
            < 0.7 => 0.625,
            _ => 0.875
        };
    }

    /// <summary>
    /// Computes an efficiency ratio combining power draw vs utilization.
    /// </summary>
    /// <param name="state">Current hardware state.</param>
    /// <returns>Normalized efficiency ratio.</returns>
    private static double ComputeEfficiencyRatio(HardwareState state)
    {
        double totalPower = state.Cpu.Power + state.Gpu.Power;
        double totalUsage = state.Cpu.Usage + state.Gpu.Usage;

        if (totalPower <= 0)
            return 0.5;

        double ratio = totalUsage / (totalPower + 1.0);
        return Math.Clamp(ratio / 2.0, 0.0, 1.0);
    }

    /// <summary>
    /// Normalizes temperature from Celsius to [0, 1] range (0-100C).
    /// </summary>
    private static double NormalizeTemperature(int temp) => Math.Clamp(temp / 100.0, 0.0, 1.0);

    /// <summary>
    /// Normalizes usage percentage to [0, 1] range.
    /// </summary>
    private static double NormalizeUsage(int usage) => Math.Clamp(usage / 100.0, 0.0, 1.0);

    /// <summary>
    /// Normalizes power in watts to [0, 1] range (0-300W).
    /// </summary>
    private static double NormalizePower(int power) => Math.Clamp(power / 300.0, 0.0, 1.0);

    /// <summary>
    /// Normalizes clock frequency to [0, 1] range (0-5000 MHz).
    /// </summary>
    private static double NormalizeClock(int clock) => Math.Clamp(clock / 5000.0, 0.0, 1.0);

    /// <summary>
    /// Normalizes VRAM usage ratio to [0, 1] range.
    /// </summary>
    private static double NormalizeVramUsage(GpuState gpu)
    {
        if (gpu.TotalVramMB <= 0)
            return 0.0;
        return Math.Clamp((double)gpu.UsedVramMB / gpu.TotalVramMB, 0.0, 1.0);
    }

    /// <summary>
    /// Normalizes battery charge to [0, 1] range.
    /// </summary>
    private static double NormalizeCharge(int charge) => Math.Clamp(charge / 100.0, 0.0, 1.0);

    /// <summary>
    /// Normalizes fan speed to [0, 1] range (0-100%).
    /// </summary>
    private static double NormalizeFanSpeed(int speed) => Math.Clamp(speed / 100.0, 0.0, 1.0);
}