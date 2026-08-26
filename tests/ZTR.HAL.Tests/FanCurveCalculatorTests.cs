using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class FanCurveCalculatorTests
{
    #region CalculateDefaultCurve Tests

    [Fact]
    public void CalculateDefaultCurve_CPU_SilentMode_ReturnsCorrectCurve()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceSilent);

        Assert.Equal(8, curve.Length);
        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
        Assert.Equal(100, curve[7].Temperature);
        Assert.Equal(100, curve[7].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_CPU_BalancedMode_ReturnsCorrectCurve()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);

        Assert.Equal(8, curve.Length);
        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
        Assert.Equal(100, curve[7].Temperature);
        Assert.Equal(100, curve[7].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_CPU_TurboMode_ReturnsCorrectCurve()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceTurbo);

        Assert.Equal(8, curve.Length);
        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(10, curve[0].Speed);
        Assert.Equal(100, curve[7].Temperature);
        Assert.Equal(100, curve[7].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_CPU_FullSpeedMode_ReturnsCorrectCurve()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceFullSpeed);

        Assert.Equal(8, curve.Length);
        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(30, curve[0].Speed);
        Assert.Equal(100, curve[7].Temperature);
        Assert.Equal(100, curve[7].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_CPU_ManualMode_ReturnsCorrectCurve()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceManual);

        Assert.Equal(8, curve.Length);
        Assert.Equal(40, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_GPU_ScaleFactorApplied()
    {
        var cpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);
        var gpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.GPU, AsusMode.PerformanceBalanced);

        Assert.Equal(cpuCurve.Length, gpuCurve.Length);
        Assert.True(gpuCurve[0].Speed >= cpuCurve[0].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_Mid_ScaleFactorApplied()
    {
        var cpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);
        var midCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.Mid, AsusMode.PerformanceBalanced);

        Assert.Equal(cpuCurve.Length, midCurve.Length);
        Assert.True(midCurve[0].Speed <= cpuCurve[0].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_XGM_ScaleFactorApplied()
    {
        var cpuCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);
        var xgmCurve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.XGM, AsusMode.PerformanceBalanced);

        Assert.Equal(cpuCurve.Length, xgmCurve.Length);
        Assert.True(xgmCurve[0].Speed >= cpuCurve[0].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_UnknownMode_FallsBackToBalanced()
    {
        var curve = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, (AsusMode)999);

        Assert.Equal(8, curve.Length);
        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
    }

    [Fact]
    public void CalculateDefaultCurve_ReturnsIndependentCopies()
    {
        var curve1 = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);
        var curve2 = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceBalanced);

        curve1[0].Speed = 999;

        Assert.NotEqual(curve1[0].Speed, curve2[0].Speed);
    }

    #endregion

    #region InterpolateCurve Tests

    [Fact]
    public void InterpolateCurve_AtFirstPoint_ReturnsFirstSpeed()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 30);

        Assert.Equal(0, speed);
    }

    [Fact]
    public void InterpolateCurve_AtLastPoint_ReturnsLastSpeed()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 100);

        Assert.Equal(100, speed);
    }

    [Fact]
    public void InterpolateCurve_BetweenPoints_InterpolatesCorrectly()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 50);

        Assert.Equal(25, speed);
    }

    [Fact]
    public void InterpolateCurve_BetweenPoints_LinearInterpolation()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 35);

        Assert.Equal(5, speed);
    }

    [Fact]
    public void InterpolateCurve_BelowMinTemp_ReturnsFirstSpeed()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 20);

        Assert.Equal(0, speed);
    }

    [Fact]
    public void InterpolateCurve_AboveMaxTemp_ReturnsLastSpeed()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 120);

        Assert.Equal(100, speed);
    }

    [Fact]
    public void InterpolateCurve_NullCurve_ReturnsZero()
    {
        int speed = FanCurveCalculator.InterpolateCurve(null!, 50);

        Assert.Equal(0, speed);
    }

    [Fact]
    public void InterpolateCurve_EmptyCurve_ReturnsZero()
    {
        int speed = FanCurveCalculator.InterpolateCurve(Array.Empty<FanCurvePoint>(), 50);

        Assert.Equal(0, speed);
    }

    [Fact]
    public void InterpolateCurve_AtExactPoint_ReturnsExactSpeed()
    {
        var curve = CreateTestCurve();

        int speed = FanCurveCalculator.InterpolateCurve(curve, 60);

        Assert.Equal(50, speed);
    }

    [Fact]
    public void InterpolateCurve_SameTemperaturePoints_UsesLaterPoint()
    {
        var curve = new[]
        {
            new FanCurvePoint { Temperature = 50, Speed = 20 },
            new FanCurvePoint { Temperature = 50, Speed = 80 }
        };

        int speed = FanCurveCalculator.InterpolateCurve(curve, 50);

        Assert.Equal(80, speed);
    }

    [Fact]
    public void InterpolateCurve_ClampsSpeedToRange()
    {
        var curve = new[]
        {
            new FanCurvePoint { Temperature = 30, Speed = -10 },
            new FanCurvePoint { Temperature = 100, Speed = 110 }
        };

        int lowSpeed = FanCurveCalculator.InterpolateCurve(curve, 30);
        int highSpeed = FanCurveCalculator.InterpolateCurve(curve, 100);

        Assert.Equal(0, lowSpeed);
        Assert.Equal(100, highSpeed);
    }

    #endregion

    #region CurveToBytes Tests

    [Fact]
    public void CurveToBytes_ValidCurve_Returns16ByteArray()
    {
        var curve = CreateTestCurve();

        byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public void CurveToBytes_ValidCurve_EncodesTemperatureAndSpeed()
    {
        var curve = CreateTestCurve();

        byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

        Assert.Equal(30, bytes[0]);
        Assert.Equal(0, bytes[1]);
        Assert.Equal(100, bytes[14]);
        Assert.Equal(100, bytes[15]);
    }

    [Fact]
    public void CurveToBytes_NullCurve_ReturnsEmptyBytes()
    {
        byte[] bytes = FanCurveCalculator.CurveToBytes(null!);

        Assert.Equal(16, bytes.Length);
        Assert.All(bytes, b => Assert.Equal((byte)0, b));
    }

    [Fact]
    public void CurveToBytes_EmptyCurve_ReturnsEmptyBytes()
    {
        byte[] bytes = FanCurveCalculator.CurveToBytes(Array.Empty<FanCurvePoint>());

        Assert.Equal(16, bytes.Length);
        Assert.All(bytes, b => Assert.Equal((byte)0, b));
    }

    [Fact]
    public void CurveToBytes_PartialCurve_PadsRemainingPoints()
    {
        var curve = new[]
        {
            new FanCurvePoint { Temperature = 40, Speed = 20 },
            new FanCurvePoint { Temperature = 80, Speed = 80 }
        };

        byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

        Assert.Equal(16, bytes.Length);
        Assert.Equal(40, bytes[0]);
        Assert.Equal(20, bytes[1]);
        Assert.Equal(80, bytes[14]);
        Assert.Equal(80, bytes[15]);
    }

    [Fact]
    public void CurveToBytes_ExceedsMaxPoints_TruncatesTo8()
    {
        var curve = new FanCurvePoint[12];
        for (int i = 0; i < 12; i++)
        {
            curve[i] = new FanCurvePoint { Temperature = 30 + i * 5, Speed = i * 10 };
        }

        byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

        Assert.Equal(16, bytes.Length);
        Assert.Equal(30, bytes[0]);
        Assert.Equal(0, bytes[1]);
    }

    [Fact]
    public void CurveToBytes_TemperatureClampedToByteRange()
    {
        var curve = new[]
        {
            new FanCurvePoint { Temperature = 300, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 },
            new FanCurvePoint { Temperature = 50, Speed = 50 }
        };

        byte[] bytes = FanCurveCalculator.CurveToBytes(curve);

        Assert.Equal(255, bytes[0]);
    }

    #endregion

    #region BytesToCurve Tests

    [Fact]
    public void BytesToCurve_ValidData_Returns8Points()
    {
        byte[] data = CreateTestBytes();

        var curve = FanCurveCalculator.BytesToCurve(data);

        Assert.Equal(8, curve.Length);
    }

    [Fact]
    public void BytesToCurve_ValidData_PreservesValues()
    {
        byte[] data = CreateTestBytes();

        var curve = FanCurveCalculator.BytesToCurve(data);

        Assert.Equal(30, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
        Assert.Equal(100, curve[7].Temperature);
        Assert.Equal(100, curve[7].Speed);
    }

    [Fact]
    public void BytesToCurve_NullData_ReturnsEmpty()
    {
        var curve = FanCurveCalculator.BytesToCurve(null!);

        Assert.Empty(curve);
    }

    [Fact]
    public void BytesToCurve_TooShort_ReturnsEmpty()
    {
        var curve = FanCurveCalculator.BytesToCurve(new byte[] { 1, 2, 3 });

        Assert.Empty(curve);
    }

    [Fact]
    public void BytesToCurve_Exactly16Bytes_ParsesAll()
    {
        byte[] data = new byte[16];
        for (int i = 0; i < 8; i++)
        {
            data[i * 2] = (byte)(40 + i * 10);
            data[i * 2 + 1] = (byte)(i * 15);
        }

        var curve = FanCurveCalculator.BytesToCurve(data);

        Assert.Equal(8, curve.Length);
        Assert.Equal(40, curve[0].Temperature);
        Assert.Equal(0, curve[0].Speed);
        Assert.Equal(110, curve[7].Temperature);
        Assert.Equal(105, curve[7].Speed);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_CurveToBytes_BytesToCurve_PreservesValues()
    {
        var original = CreateTestCurve();

        byte[] bytes = FanCurveCalculator.CurveToBytes(original);
        var result = FanCurveCalculator.BytesToCurve(bytes);

        Assert.Equal(original.Length, result.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Temperature, result[i].Temperature);
            Assert.Equal(original[i].Speed, result[i].Speed);
        }
    }

    [Fact]
    public void RoundTrip_DefaultCurve_RoundTripPreservesValues()
    {
        var original = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, AsusMode.PerformanceTurbo);

        byte[] bytes = FanCurveCalculator.CurveToBytes(original);
        var result = FanCurveCalculator.BytesToCurve(bytes);

        Assert.Equal(original.Length, result.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Temperature, result[i].Temperature);
            Assert.Equal(original[i].Speed, result[i].Speed);
        }
    }

    [Fact]
    public void RoundTrip_AllModes_RoundTripPreservesValues()
    {
        var modes = Enum.GetValues<AsusMode>();

        foreach (var mode in modes)
        {
            var original = FanCurveCalculator.CalculateDefaultCurve(AsusFan.CPU, mode);
            byte[] bytes = FanCurveCalculator.CurveToBytes(original);
            var result = FanCurveCalculator.BytesToCurve(bytes);

            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i].Temperature, result[i].Temperature);
                Assert.Equal(original[i].Speed, result[i].Speed);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static FanCurvePoint[] CreateTestCurve()
    {
        return new[]
        {
            new FanCurvePoint { Temperature = 30, Speed = 0 },
            new FanCurvePoint { Temperature = 40, Speed = 10 },
            new FanCurvePoint { Temperature = 50, Speed = 25 },
            new FanCurvePoint { Temperature = 60, Speed = 50 },
            new FanCurvePoint { Temperature = 70, Speed = 60 },
            new FanCurvePoint { Temperature = 80, Speed = 75 },
            new FanCurvePoint { Temperature = 90, Speed = 90 },
            new FanCurvePoint { Temperature = 100, Speed = 100 }
        };
    }

    private static byte[] CreateTestBytes()
    {
        return new byte[]
        {
            30, 0, 40, 10, 50, 25, 60, 50,
            70, 60, 80, 75, 90, 90, 100, 100
        };
    }

    #endregion
}