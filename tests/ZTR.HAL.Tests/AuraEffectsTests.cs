using ZTR.HAL;

namespace ZTR.HAL.Tests;

public class AuraEffectsTests
{
    private readonly AuraEffects _effects;

    public AuraEffectsTests()
    {
        _effects = new AuraEffects();
    }

    #region Heatmap Tests

    [Fact]
    public void CalculateHeatmapColor_LowTemp_ReturnsBlue()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(40, 35);

        Assert.Equal(0, r);
        Assert.True(b >= 200);
    }

    [Fact]
    public void CalculateHeatmapColor_MediumTemp_ReturnsGreen()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(70, 60);

        Assert.True(g >= 200);
    }

    [Fact]
    public void CalculateHeatmapColor_HighTemp_ReturnsRed()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(95, 90);

        Assert.True(r >= 200);
    }

    [Fact]
    public void CalculateHeatmapColor_VeryHighTemp_ReturnsBrightRed()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(100, 95);

        Assert.Equal(255, r);
    }

    [Fact]
    public void CalculateHeatmapColor_ZeroTemp_ReturnsBlue()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(0, 0);

        Assert.Equal(0, r);
        Assert.True(b >= 200);
    }

    [Fact]
    public void CalculateHeatmapColor_CpuTempUsedAsMax()
    {
        var (r1, g1, b1) = _effects.CalculateHeatmapColor(80, 40);
        var (r2, g2, b2) = _effects.CalculateHeatmapColor(40, 80);

        Assert.Equal(r1, r2);
        Assert.Equal(g1, g2);
        Assert.Equal(b1, b2);
    }

    [Fact]
    public void CalculateHeatmapColor_BothZero_MaxIsZero()
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(0, 0);

        Assert.True(r <= 50);
        Assert.True(b >= 200);
    }

    #endregion

    #region Ambient Tests

    [Fact]
    public void CalculateAmbientColor_White_ReturnsWhite()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((255, 255, 255));

        Assert.True(r >= 200);
        Assert.True(g >= 200);
        Assert.True(b >= 200);
    }

    [Fact]
    public void CalculateAmbientColor_Black_ReturnsDim()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((0, 0, 0));

        Assert.True(r <= 50);
        Assert.True(g <= 50);
        Assert.True(b <= 50);
    }

    [Fact]
    public void CalculateAmbientColor_PureRed_PreservesRed()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((255, 0, 0));

        Assert.True(r > g);
        Assert.True(r > b);
    }

    [Fact]
    public void CalculateAmbientColor_PureGreen_PreservesGreen()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((0, 255, 0));

        Assert.True(g > r);
        Assert.True(g > b);
    }

    [Fact]
    public void CalculateAmbientColor_PureBlue_PreservesBlue()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((0, 0, 255));

        Assert.True(b > r);
        Assert.True(b > g);
    }

    [Fact]
    public void CalculateAmbientColor_NormalizedValues()
    {
        var (r, g, b) = _effects.CalculateAmbientColor((128, 128, 128));

        Assert.True(r > 0);
        Assert.True(g > 0);
        Assert.True(b > 0);
    }

    #endregion

    #region Battery Tests

    [Fact]
    public void CalculateBatteryColor_LowCharge_Red()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(10);

        Assert.True(r >= 200);
    }

    [Fact]
    public void CalculateBatteryColor_MediumCharge_YellowGreen()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(40);

        Assert.True(g >= 150);
    }

    [Fact]
    public void CalculateBatteryColor_HighCharge_Green()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(70);

        Assert.True(g >= 200);
    }

    [Fact]
    public void CalculateBatteryColor_FullCharge_BrightGreen()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(100);

        Assert.Equal(0, r);
        Assert.Equal(255, g);
        Assert.True(b >= 0);
    }

    [Fact]
    public void CalculateBatteryColor_ZeroCharge_DarkRed()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(0);

        Assert.True(r >= 200);
        Assert.True(g <= 50);
    }

    [Fact]
    public void CalculateBatteryColor_ChargeOutOfRange_Clamped()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(150);

        Assert.True(g >= 200);
    }

    [Fact]
    public void CalculateBatteryColor_NegativeCharge_Clamped()
    {
        var (r, g, b) = _effects.CalculateBatteryColor(-20);

        Assert.True(r >= 200);
    }

    #endregion

    #region Audio Tests

    [Fact]
    public void CalculateAudioColor_Silent_DimBlue()
    {
        var (r, g, b) = _effects.CalculateAudioColor(10);

        Assert.Equal(0, r);
        Assert.True(b >= 100);
    }

    [Fact]
    public void CalculateAudioColor_Quiet_Cyan()
    {
        var (r, g, b) = _effects.CalculateAudioColor(40);

        Assert.True(g > 100);
        Assert.True(b > 100);
    }

    [Fact]
    public void CalculateAudioColor_Normal_Green()
    {
        var (r, g, b) = _effects.CalculateAudioColor(100);

        Assert.True(g >= 200);
    }

    [Fact]
    public void CalculateAudioColor_Loud_Yellow()
    {
        var (r, g, b) = _effects.CalculateAudioColor(160);

        Assert.True(r >= 100);
        Assert.True(g >= 200);
    }

    [Fact]
    public void CalculateAudioColor_Peak_White()
    {
        var (r, g, b) = _effects.CalculateAudioColor(250);

        Assert.True(r >= 200);
        Assert.True(g >= 200);
        Assert.True(b >= 200);
    }

    [Fact]
    public void CalculateAudioColor_ZeroLevel_Dark()
    {
        var (r, g, b) = _effects.CalculateAudioColor(0);

        Assert.True(r <= 50);
        Assert.True(g <= 100);
        Assert.True(b >= 100);
    }

    [Fact]
    public void CalculateAudioColor_MaxLevel_White()
    {
        var (r, g, b) = _effects.CalculateAudioColor(255);

        Assert.Equal(255, r);
        Assert.True(g >= 200);
        Assert.True(b >= 200);
    }

    #endregion

    #region GenerateHeatmapZones Tests

    [Fact]
    public void GenerateHeatmapZones_ReturnsCorrectCount()
    {
        var colors = _effects.GenerateHeatmapZones(70, 60, 4);

        Assert.Equal(4, colors.Length);
    }

    [Fact]
    public void GenerateHeatmapZones_AllZonesHaveValidColors()
    {
        var colors = _effects.GenerateHeatmapZones(80, 70, 4);

        foreach (var (r, g, b) in colors)
        {
            Assert.True(r >= 0 && r <= 255);
            Assert.True(g >= 0 && g <= 255);
            Assert.True(b >= 0 && b <= 255);
        }
    }

    [Fact]
    public void GenerateHeatmapZones_SingleZone()
    {
        var colors = _effects.GenerateHeatmapZones(50, 45, 1);

        Assert.Single(colors);
    }

    #endregion

    #region GenerateBatteryZones Tests

    [Fact]
    public void GenerateBatteryZones_ReturnsCorrectCount()
    {
        var colors = _effects.GenerateBatteryZones(50, 4);

        Assert.Equal(4, colors.Length);
    }

    [Fact]
    public void GenerateBatteryZones_AllZonesHaveValidColors()
    {
        var colors = _effects.GenerateBatteryZones(80, 4);

        foreach (var (r, g, b) in colors)
        {
            Assert.True(r >= 0 && r <= 255);
            Assert.True(g >= 0 && g <= 255);
            Assert.True(b >= 0 && b <= 255);
        }
    }

    #endregion

    #region GenerateAudioZones Tests

    [Fact]
    public void GenerateAudioZones_ReturnsCorrectCount()
    {
        var levels = new[] { 100, 150, 50, 200 };
        var colors = _effects.GenerateAudioZones(levels, 4);

        Assert.Equal(4, colors.Length);
    }

    [Fact]
    public void GenerateAudioZones_UsesLevelArray()
    {
        var levels = new[] { 255, 0, 128, 64 };
        var colors = _effects.GenerateAudioZones(levels, 4);

        Assert.Equal(4, colors.Length);
    }

    [Fact]
    public void GenerateAudioZones_LevelsExceedCount_UsesZeroForMissing()
    {
        var levels = new[] { 255, 128 };
        var colors = _effects.GenerateAudioZones(levels, 4);

        Assert.Equal(4, colors.Length);
        var (r3, g3, b3) = colors[2];
        Assert.True(r3 >= 0);
    }

    #endregion

    #region GenerateAmbientZones Tests

    [Fact]
    public void GenerateAmbientZones_ReturnsCorrectCount()
    {
        var screenColors = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0), (0, 255, 0), (0, 0, 255), (128, 128, 128)
        };
        var colors = _effects.GenerateAmbientZones(screenColors, 4);

        Assert.Equal(4, colors.Length);
    }

    [Fact]
    public void GenerateAmbientZones_UsesScreenColors()
    {
        var screenColors = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0), (0, 255, 0)
        };
        var colors = _effects.GenerateAmbientZones(screenColors, 2);

        Assert.Equal(2, colors.Length);
    }

    [Fact]
    public void GenerateAmbientZones_MoreZonesThanColors_DefaultsToBlack()
    {
        var screenColors = new (byte R, byte G, byte B)[]
        {
            (255, 255, 255)
        };
        var colors = _effects.GenerateAmbientZones(screenColors, 3);

        Assert.Equal(3, colors.Length);
    }

    #endregion
}