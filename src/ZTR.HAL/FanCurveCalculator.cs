using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Provides fan curve calculation, interpolation, and conversion utilities
/// for ASUS device fan control. Handles the 16-byte ACPI fan curve format
/// consisting of 8 temperature/speed point pairs.
/// </summary>
public static class FanCurveCalculator
{
    /// <summary>
    /// The number of point pairs in an ACPI fan curve.
    /// </summary>
    public const int CurvePointCount = 8;

    /// <summary>
    /// The size in bytes of a serialized fan curve.
    /// </summary>
    public const int CurveByteSize = 16;

    private static readonly FanCurvePoint[] SilentCpuCurve =
    {
        new() { Temperature = 30, Speed = 0 },
        new() { Temperature = 40, Speed = 10 },
        new() { Temperature = 50, Speed = 20 },
        new() { Temperature = 60, Speed = 35 },
        new() { Temperature = 70, Speed = 50 },
        new() { Temperature = 80, Speed = 65 },
        new() { Temperature = 90, Speed = 80 },
        new() { Temperature = 100, Speed = 100 }
    };

    private static readonly FanCurvePoint[] BalancedCpuCurve =
    {
        new() { Temperature = 30, Speed = 0 },
        new() { Temperature = 40, Speed = 15 },
        new() { Temperature = 50, Speed = 30 },
        new() { Temperature = 60, Speed = 45 },
        new() { Temperature = 70, Speed = 60 },
        new() { Temperature = 80, Speed = 75 },
        new() { Temperature = 90, Speed = 90 },
        new() { Temperature = 100, Speed = 100 }
    };

    private static readonly FanCurvePoint[] TurboCpuCurve =
    {
        new() { Temperature = 30, Speed = 10 },
        new() { Temperature = 40, Speed = 25 },
        new() { Temperature = 50, Speed = 45 },
        new() { Temperature = 60, Speed = 60 },
        new() { Temperature = 70, Speed = 75 },
        new() { Temperature = 80, Speed = 85 },
        new() { Temperature = 90, Speed = 95 },
        new() { Temperature = 100, Speed = 100 }
    };

    private static readonly FanCurvePoint[] FullSpeedCpuCurve =
    {
        new() { Temperature = 30, Speed = 30 },
        new() { Temperature = 40, Speed = 45 },
        new() { Temperature = 50, Speed = 60 },
        new() { Temperature = 60, Speed = 70 },
        new() { Temperature = 70, Speed = 80 },
        new() { Temperature = 80, Speed = 90 },
        new() { Temperature = 90, Speed = 100 },
        new() { Temperature = 100, Speed = 100 }
    };

    private static readonly FanCurvePoint[] ManualCpuCurve =
    {
        new() { Temperature = 40, Speed = 0 },
        new() { Temperature = 50, Speed = 0 },
        new() { Temperature = 60, Speed = 0 },
        new() { Temperature = 70, Speed = 25 },
        new() { Temperature = 80, Speed = 50 },
        new() { Temperature = 90, Speed = 75 },
        new() { Temperature = 95, Speed = 100 },
        new() { Temperature = 100, Speed = 100 }
    };

    /// <summary>
    /// Gets the default fan curve for a given fan and performance mode.
    /// </summary>
    /// <param name="fan">The fan type (CPU, GPU, Mid, XGM).</param>
    /// <param name="mode">The performance mode.</param>
    /// <returns>An array of <see cref="FanCurvePoint"/> representing the default curve.</returns>
    public static FanCurvePoint[] CalculateDefaultCurve(AsusFan fan, AsusMode mode)
    {
        var baseCurve = mode switch
        {
            AsusMode.PerformanceSilent => CloneCurve(SilentCpuCurve),
            AsusMode.PerformanceBalanced => CloneCurve(BalancedCpuCurve),
            AsusMode.PerformanceTurbo => CloneCurve(TurboCpuCurve),
            AsusMode.PerformanceFullSpeed => CloneCurve(FullSpeedCpuCurve),
            AsusMode.PerformanceManual => CloneCurve(ManualCpuCurve),
            _ => CloneCurve(BalancedCpuCurve)
        };

        if (fan == AsusFan.GPU)
        {
            return ScaleCurve(baseCurve, 1.1);
        }

        if (fan == AsusFan.Mid)
        {
            return ScaleCurve(baseCurve, 0.9);
        }

        if (fan == AsusFan.XGM)
        {
            return ScaleCurve(baseCurve, 1.2);
        }

        return baseCurve;
    }

    /// <summary>
    /// Interpolates the fan speed for a given temperature using the specified curve points.
    /// Uses linear interpolation between adjacent curve points.
    /// </summary>
    /// <param name="points">The fan curve points defining the curve.</param>
    /// <param name="temperature">The temperature in degrees Celsius.</param>
    /// <returns>The interpolated fan speed as a percentage (0-100).</returns>
    public static int InterpolateCurve(FanCurvePoint[] points, int temperature)
    {
        if (points == null || points.Length == 0)
            return 0;

        if (temperature <= points[0].Temperature)
        {
            if (points.Length > 1 && points[0].Temperature == points[1].Temperature)
                return Math.Clamp(points[1].Speed, 0, 100);
            return Math.Clamp(points[0].Speed, 0, 100);
        }

        if (temperature >= points[^1].Temperature)
            return Math.Clamp(points[^1].Speed, 0, 100);

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (temperature >= points[i].Temperature && temperature <= points[i + 1].Temperature)
            {
                int tempDiff = points[i + 1].Temperature - points[i].Temperature;
                int speedDiff = points[i + 1].Speed - points[i].Speed;

                if (tempDiff == 0)
                    return Math.Clamp(points[i + 1].Speed, 0, 100);

                double ratio = (double)(temperature - points[i].Temperature) / tempDiff;
                double interpolated = points[i].Speed + ratio * speedDiff;
                return (int)Math.Round(Math.Clamp(interpolated, 0, 100));
            }
        }

        return Math.Clamp(points[^1].Speed, 0, 100);
    }

    /// <summary>
    /// Converts an array of <see cref="FanCurvePoint"/> to the 16-byte ACPI format.
    /// Each point is represented as two bytes: temperature (index 0, 2, 4, ...) and speed (index 1, 3, 5, ...).
    /// </summary>
    /// <param name="points">The fan curve points to convert.</param>
    /// <returns>A 16-byte array representing the fan curve in ACPI format.</returns>
    public static byte[] CurveToBytes(FanCurvePoint[] points)
    {
        byte[] bytes = new byte[CurveByteSize];

        if (points == null || points.Length == 0)
            return bytes;

        int count = Math.Min(points.Length, CurvePointCount);

        for (int i = 0; i < count; i++)
        {
            bytes[i * 2] = (byte)Math.Clamp(points[i].Temperature, 0, 255);
            bytes[i * 2 + 1] = (byte)Math.Clamp(points[i].Speed, 0, 100);
        }

        if (count < CurvePointCount)
        {
            for (int i = count; i < CurvePointCount; i++)
            {
                bytes[i * 2] = (byte)Math.Clamp(points[^1].Temperature, 0, 255);
                bytes[i * 2 + 1] = (byte)Math.Clamp(points[^1].Speed, 0, 100);
            }
        }

        return bytes;
    }

    /// <summary>
    /// Converts a 16-byte ACPI fan curve buffer back to an array of <see cref="FanCurvePoint"/>.
    /// </summary>
    /// <param name="data">The 16-byte ACPI fan curve data.</param>
    /// <returns>An array of <see cref="FanCurvePoint"/> parsed from the byte data.</returns>
    public static FanCurvePoint[] BytesToCurve(byte[] data)
    {
        if (data == null || data.Length < CurveByteSize)
            return Array.Empty<FanCurvePoint>();

        var points = new FanCurvePoint[CurvePointCount];

        for (int i = 0; i < CurvePointCount; i++)
        {
            points[i] = new FanCurvePoint
            {
                Temperature = data[i * 2],
                Speed = data[i * 2 + 1]
            };
        }

        return points;
    }

    /// <summary>
    /// Creates a deep copy of a curve array.
    /// </summary>
    private static FanCurvePoint[] CloneCurve(FanCurvePoint[] curve)
    {
        var clone = new FanCurvePoint[curve.Length];
        for (int i = 0; i < curve.Length; i++)
        {
            clone[i] = new FanCurvePoint
            {
                Temperature = curve[i].Temperature,
                Speed = curve[i].Speed
            };
        }
        return clone;
    }

    /// <summary>
    /// Scales a curve's speed values by the specified multiplier.
    /// </summary>
    private static FanCurvePoint[] ScaleCurve(FanCurvePoint[] curve, double multiplier)
    {
        var scaled = new FanCurvePoint[curve.Length];
        for (int i = 0; i < curve.Length; i++)
        {
            scaled[i] = new FanCurvePoint
            {
                Temperature = curve[i].Temperature,
                Speed = (int)Math.Round(Math.Clamp(curve[i].Speed * multiplier, 0, 100))
            };
        }
        return scaled;
    }
}