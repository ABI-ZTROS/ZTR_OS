using Moq;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.HAL.Tests;

public class AuraLightingTests
{
    private Mock<IHidReportWriter> _mockWriter;
    private AsusHid _hid;
    private AuraLighting _lighting;

    public AuraLightingTests()
    {
        _mockWriter = new Mock<IHidReportWriter>();
        _hid = new AsusHid(_mockWriter.Object);
        _lighting = new AuraLighting(_hid);
    }

    private void SetupWriterVerifiable()
    {
        _hid.RegisterStream(0x1a30);
        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();
    }

    #region AuraMessage Format Tests

    [Fact]
    public void AuraMessage_Static_Correct17ByteFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Static, AuraZone.Keyboard, 255, 128, 64, 0, 0, r2: 255, g2: 128, b2: 64);

        Assert.Equal(17, msg.Length);
        Assert.Equal(0x5D, msg[0]);
        Assert.Equal(0xB3, msg[1]);
        Assert.Equal((byte)AuraZone.Keyboard, msg[2]);
        Assert.Equal((byte)AuraMode.Static, msg[3]);
        Assert.Equal(255, msg[4]);
        Assert.Equal(128, msg[5]);
        Assert.Equal(64, msg[6]);
        Assert.Equal(0, msg[7]);
        Assert.Equal(0, msg[8]);
        Assert.Equal(0, msg[9]);
        Assert.Equal(255, msg[10]);
        Assert.Equal(128, msg[11]);
        Assert.Equal(64, msg[12]);
        Assert.Equal(0x00, msg[13]);
        Assert.Equal(0x00, msg[14]);
        Assert.Equal(0x00, msg[15]);
        Assert.Equal(0x00, msg[16]);
    }

    [Fact]
    public void AuraMessage_Breathe_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Breathe, AuraZone.Body, 100, 200, 50, 128, 0);

        Assert.Equal(17, msg.Length);
        Assert.Equal(0x5D, msg[0]);
        Assert.Equal(0xB3, msg[1]);
        Assert.Equal((byte)AuraZone.Body, msg[2]);
        Assert.Equal((byte)AuraMode.Breathe, msg[3]);
        Assert.Equal(100, msg[4]);
        Assert.Equal(200, msg[5]);
        Assert.Equal(50, msg[6]);
        Assert.Equal(128, msg[7]);
        Assert.Equal(0, msg[8]);
    }

    [Fact]
    public void AuraMessage_WithRandom_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Star, AuraZone.Keyboard, 255, 0, 0, 100, 0, random: 42);

        Assert.Equal(17, msg.Length);
        Assert.Equal((byte)AuraMode.Star, msg[3]);
        Assert.Equal(100, msg[7]);
        Assert.Equal(0, msg[8]);
        Assert.Equal(42, msg[9]);
    }

    [Fact]
    public void AuraMessage_WithDirection_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Rainbow, AuraZone.Keyboard, 0, 0, 0, 50, direction: 2);

        Assert.Equal(17, msg.Length);
        Assert.Equal((byte)AuraMode.Rainbow, msg[3]);
        Assert.Equal(50, msg[7]);
        Assert.Equal(2, msg[8]);
    }

    [Fact]
    public void AuraMessage_WithSecondaryColor_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Comet, AuraZone.Keyboard, 255, 0, 0, 100, 1,
            r2: 0, g2: 255, b2: 0);

        Assert.Equal(17, msg.Length);
        Assert.Equal((byte)AuraMode.Comet, msg[3]);
        Assert.Equal(1, msg[8]);
        Assert.Equal(0, msg[10]);
        Assert.Equal(255, msg[11]);
        Assert.Equal(0, msg[12]);
    }

    [Fact]
    public void AuraMessage_AllZeros_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Static, AuraZone.Keyboard, 0, 0, 0, 0, 0);

        Assert.Equal(17, msg.Length);
        Assert.Equal(0x5D, msg[0]);
        Assert.Equal(0xB3, msg[1]);
        Assert.Equal(0, msg[4]);
        Assert.Equal(0, msg[5]);
        Assert.Equal(0, msg[6]);
        Assert.Equal(0, msg[10]);
        Assert.Equal(0, msg[11]);
        Assert.Equal(0, msg[12]);
    }

    [Fact]
    public void AuraMessage_AllMaxValues_CorrectFormat()
    {
        var msg = AuraLighting.AuraMessage(AuraMode.Flash, AuraZone.Mouse, 255, 255, 255, 255, 3,
            random: 255, r2: 255, g2: 255, b2: 255);

        Assert.Equal(17, msg.Length);
        Assert.Equal(255, msg[4]);
        Assert.Equal(255, msg[5]);
        Assert.Equal(255, msg[6]);
        Assert.Equal(255, msg[7]);
        Assert.Equal(3, msg[8]);
        Assert.Equal(255, msg[9]);
        Assert.Equal(255, msg[10]);
        Assert.Equal(255, msg[11]);
        Assert.Equal(255, msg[12]);
    }

    #endregion

    #region All AuraMode Enum Value Tests

    public static TheoryData<AuraMode> AllModes => new()
    {
        AuraMode.Static,
        AuraMode.Breathe,
        AuraMode.ColorCycle,
        AuraMode.Rainbow,
        AuraMode.Strobe,
        AuraMode.Star,
        AuraMode.Rain,
        AuraMode.Highlight,
        AuraMode.Laser,
        AuraMode.Ripple,
        AuraMode.Comet,
        AuraMode.Flash,
        AuraMode.Heatmap,
        AuraMode.GPUMode,
        AuraMode.Ambient,
        AuraMode.Battery,
        AuraMode.Gradient,
        AuraMode.Audio,
        AuraMode.AudioPulse
    };

    [Theory]
    [MemberData(nameof(AllModes))]
    public void AuraMessage_AllModes_Produce17ByteOutput(AuraMode mode)
    {
        var msg = AuraLighting.AuraMessage(mode, AuraZone.Keyboard, 255, 128, 64, 50, 0);

        Assert.Equal(17, msg.Length);
        Assert.Equal(0x5D, msg[0]);
        Assert.Equal(0xB3, msg[1]);
        Assert.Equal((byte)AuraZone.Keyboard, msg[2]);
        Assert.Equal((byte)mode, msg[3]);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public void SetMode_AllModes_SendsCommand(AuraMode mode)
    {
        SetupWriterVerifiable();

        _lighting.SetMode(mode, AuraZone.Keyboard, 255, 0, 0, 50, 0);

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeastOnce);
    }

    #endregion

    #region Static Mode Tests

    [Fact]
    public void SetStatic_SendsCorrectMessage()
    {
        SetupWriterVerifiable();

        _lighting.SetStatic(AuraZone.Keyboard, 255, 0, 0);

        Assert.Equal(AuraMode.Static, _lighting.CurrentMode);
        Assert.Equal(AuraZone.Keyboard, _lighting.CurrentZone);
        Assert.Equal((255, (byte)0, (byte)0), _lighting.CurrentColor);
    }

    [Fact]
    public void SetStatic_RedColor()
    {
        SetupWriterVerifiable();

        _lighting.SetStatic(AuraZone.Body, 255, 0, 0);
        Assert.Equal((255, (byte)0, (byte)0), _lighting.CurrentColor);
    }

    [Fact]
    public void SetStatic_GreenColor()
    {
        SetupWriterVerifiable();

        _lighting.SetStatic(AuraZone.Body, 0, 255, 0);
        Assert.Equal(((byte)0, 255, (byte)0), _lighting.CurrentColor);
    }

    [Fact]
    public void SetStatic_BlueColor()
    {
        SetupWriterVerifiable();

        _lighting.SetStatic(AuraZone.Body, 0, 0, 255);
        Assert.Equal(((byte)0, (byte)0, 255), _lighting.CurrentColor);
    }

    #endregion

    #region Breathe Mode Tests

    [Fact]
    public void SetBreathe_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetBreathe(AuraZone.Keyboard, 128, 64, 200, 100);

        Assert.Equal(AuraMode.Breathe, _lighting.CurrentMode);
    }

    #endregion

    #region ColorCycle Mode Tests

    [Fact]
    public void SetColorCycle_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetColorCycle(AuraZone.Keyboard, 80);

        Assert.Equal(AuraMode.ColorCycle, _lighting.CurrentMode);
        Assert.Equal(80, _lighting.CurrentSpeed);
    }

    #endregion

    #region Rainbow Mode Tests

    [Fact]
    public void SetRainbow_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetRainbow(AuraZone.Keyboard, 60, 1);

        Assert.Equal(AuraMode.Rainbow, _lighting.CurrentMode);
    }

    #endregion

    #region Strobe Mode Tests

    [Fact]
    public void SetStrobe_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetStrobe(AuraZone.Keyboard, 255, 255, 255, 200);

        Assert.Equal(AuraMode.Strobe, _lighting.CurrentMode);
    }

    #endregion

    #region PerKey Effect Mode Tests

    [Fact]
    public void SetStar_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetStar(AuraZone.Keyboard, 255, 200, 100, 50, 25);

        Assert.Equal(AuraMode.Star, _lighting.CurrentMode);
    }

    [Fact]
    public void SetRain_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetRain(AuraZone.Keyboard, 0, 255, 128, 60, 30);

        Assert.Equal(AuraMode.Rain, _lighting.CurrentMode);
    }

    [Fact]
    public void SetHighlight_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetHighlight(AuraZone.Keyboard, 255, 255, 0, 80);

        Assert.Equal(AuraMode.Highlight, _lighting.CurrentMode);
    }

    [Fact]
    public void SetLaser_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetLaser(AuraZone.Keyboard, 255, 0, 128, 100, 2);

        Assert.Equal(AuraMode.Laser, _lighting.CurrentMode);
    }

    [Fact]
    public void SetRipple_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetRipple(AuraZone.Keyboard, 0, 0, 255, 40, 15);

        Assert.Equal(AuraMode.Ripple, _lighting.CurrentMode);
    }

    [Fact]
    public void SetComet_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetComet(AuraZone.Keyboard, 255, 128, 0, 70, 1);

        Assert.Equal(AuraMode.Comet, _lighting.CurrentMode);
    }

    [Fact]
    public void SetFlash_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetFlash(AuraZone.Keyboard, 255, 0, 0, 150, 60);

        Assert.Equal(AuraMode.Flash, _lighting.CurrentMode);
    }

    #endregion

    #region Gradient Mode Tests

    [Fact]
    public void SetGradient_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetGradient(AuraZone.Keyboard, 255, 0, 128, 90, 3);

        Assert.Equal(AuraMode.Gradient, _lighting.CurrentMode);
    }

    #endregion

    #region GPUMode Tests

    [Fact]
    public void SetGPUMode_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetGPUMode(AuraZone.Keyboard);

        Assert.Equal(AuraMode.GPUMode, _lighting.CurrentMode);
    }

    #endregion

    #region ZoneTest Mode Tests

    [Fact]
    public void SetZoneTest_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetZoneTest(AuraZone.Body, 255, 0, 0);

        Assert.Equal(AuraMode.ZoneTest, _lighting.CurrentMode);
        Assert.Equal(AuraZone.Body, _lighting.CurrentZone);
    }

    #endregion

    #region Heatmap Custom Effect Tests

    [Fact]
    public void SetHeatmap_CoolTemp_BlueColor()
    {
        SetupWriterVerifiable();

        _lighting.SetHeatmap(AuraZone.Keyboard, 40, 35);

        Assert.Equal(AuraMode.Heatmap, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.Equal(0, r);
        Assert.True(b >= 200);
    }

    [Fact]
    public void SetHeatmap_WarmTemp_GreenYellow()
    {
        SetupWriterVerifiable();

        _lighting.SetHeatmap(AuraZone.Keyboard, 70, 65);

        Assert.Equal(AuraMode.Heatmap, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(g >= 200);
    }

    [Fact]
    public void SetHeatmap_HotTemp_RedColor()
    {
        SetupWriterVerifiable();

        _lighting.SetHeatmap(AuraZone.Keyboard, 95, 90);

        Assert.Equal(AuraMode.Heatmap, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(r >= 200);
    }

    #endregion

    #region Ambient Custom Effect Tests

    [Fact]
    public void SetAmbient_WhiteColor()
    {
        SetupWriterVerifiable();

        _lighting.SetAmbient(AuraZone.Keyboard, (255, 255, 255));

        Assert.Equal(AuraMode.Ambient, _lighting.CurrentMode);
    }

    [Fact]
    public void SetAmbient_BlackColor()
    {
        SetupWriterVerifiable();

        _lighting.SetAmbient(AuraZone.Keyboard, (0, 0, 0));

        Assert.Equal(AuraMode.Ambient, _lighting.CurrentMode);
    }

    [Fact]
    public void SetAmbient_RedColor()
    {
        SetupWriterVerifiable();

        _lighting.SetAmbient(AuraZone.Keyboard, (255, 0, 0));

        Assert.Equal(AuraMode.Ambient, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(r > g);
    }

    #endregion

    #region Battery Custom Effect Tests

    [Fact]
    public void SetBattery_LowCharge_RedColor()
    {
        SetupWriterVerifiable();

        _lighting.SetBattery(AuraZone.Keyboard, 10);

        Assert.Equal(AuraMode.Battery, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(r >= 200);
    }

    [Fact]
    public void SetBattery_MediumCharge_YellowGreen()
    {
        SetupWriterVerifiable();

        _lighting.SetBattery(AuraZone.Keyboard, 35);

        Assert.Equal(AuraMode.Battery, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(g > r);
    }

    [Fact]
    public void SetBattery_HighCharge_GreenColor()
    {
        SetupWriterVerifiable();

        _lighting.SetBattery(AuraZone.Keyboard, 90);

        Assert.Equal(AuraMode.Battery, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(g >= 200);
    }

    [Fact]
    public void SetBattery_FullCharge_BrightGreen()
    {
        SetupWriterVerifiable();

        _lighting.SetBattery(AuraZone.Keyboard, 100);

        var (r, g, b) = _lighting.CurrentColor;
        Assert.Equal(0, r);
        Assert.Equal(255, g);
    }

    #endregion

    #region Audio Custom Effect Tests

    [Fact]
    public void SetAudio_Silent_DimBlue()
    {
        SetupWriterVerifiable();

        _lighting.SetAudio(AuraZone.Keyboard, 10);

        Assert.Equal(AuraMode.Audio, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(b > r);
    }

    [Fact]
    public void SetAudio_Loud_RedOrange()
    {
        SetupWriterVerifiable();

        _lighting.SetAudio(AuraZone.Keyboard, 230);

        Assert.Equal(AuraMode.Audio, _lighting.CurrentMode);
        var (r, g, b) = _lighting.CurrentColor;
        Assert.True(r >= 200);
    }

    [Fact]
    public void SetAudioPulse_SetsCorrectMode()
    {
        SetupWriterVerifiable();

        _lighting.SetAudioPulse(AuraZone.Keyboard, 128, 60);

        Assert.Equal(AuraMode.AudioPulse, _lighting.CurrentMode);
        Assert.Equal(60, _lighting.CurrentSpeed);
    }

    #endregion

    #region Direct Mode Tests

    [Fact]
    public void SetDirectMode_ValidData_SendsFeatureReport()
    {
        foreach (var pid in AsusHid.MainAuraPids)
            _hid.RegisterStream(pid);

        _mockWriter.Setup(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()))
            .Verifiable();

        byte[] ledColors = new byte[AuraLighting.PerKeyLedCount * 3];
        for (int i = 0; i < ledColors.Length; i++)
            ledColors[i] = (byte)(i % 256);

        bool result = _lighting.SetDirectMode(ledColors);

        Assert.True(result);
    }

    [Fact]
    public void SetDirectMode_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _lighting.SetDirectMode(null!));
    }

    [Fact]
    public void SetDirectMode_WrongLength_ThrowsArgumentException()
    {
        byte[] shortData = new byte[100];
        Assert.Throws<ArgumentException>(() => _lighting.SetDirectMode(shortData));
    }

    [Fact]
    public void SetDirectMode_CorrectLength_Is534Bytes()
    {
        Assert.Equal(534, AuraLighting.PerKeyLedCount * 3);
    }

    [Fact]
    public void SetDirectMode4Zone_ValidData_SendsAllZones()
    {
        SetupWriterVerifiable();

        var zoneColors = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0),
            (0, 255, 0),
            (0, 0, 255),
            (255, 255, 0)
        };

        _lighting.SetDirectMode4Zone(zoneColors);

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeast(4));
    }

    [Fact]
    public void SetDirectMode4Zone_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _lighting.SetDirectMode4Zone(null!));
    }

    [Fact]
    public void SetDirectMode4Zone_WrongCount_ThrowsArgumentException()
    {
        var shortData = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0),
            (0, 255, 0)
        };

        Assert.Throws<ArgumentException>(() => _lighting.SetDirectMode4Zone(shortData));
    }

    #endregion

    #region Zone Management Tests

    [Fact]
    public void SetRegion_ValidRegion_SendsCommand()
    {
        SetupWriterVerifiable();

        _lighting.SetRegion(0, AuraMode.Static, 255, 0, 0);

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeastOnce);
    }

    [Fact]
    public void SetRegion_AllValidRegions_SendCommands()
    {
        SetupWriterVerifiable();

        for (int i = 0; i < AuraLighting.MaxRegions; i++)
        {
            _lighting.SetRegion(i, AuraMode.Breathe, 128, 128, 128, 50);
        }

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeast(4));
    }

    [Fact]
    public void SetRegion_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _lighting.SetRegion(-1, AuraMode.Static, 0, 0, 0));
    }

    [Fact]
    public void SetRegion_IndexTooLarge_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _lighting.SetRegion(AuraLighting.MaxRegions, AuraMode.Static, 0, 0, 0));
    }

    [Fact]
    public void MaxRegions_Is4()
    {
        Assert.Equal(4, AuraLighting.MaxRegions);
    }

    #endregion

    #region AuraSync Tests

    [Fact]
    public void SetAuraSync_Enabled_SendsSyncCommand()
    {
        SetupWriterVerifiable();

        _lighting.SetAuraSync(true);

        Assert.True(_lighting.IsAuraSyncEnabled);
    }

    [Fact]
    public void SetAuraSync_Disabled_SendsDisableCommand()
    {
        SetupWriterVerifiable();

        _lighting.SetAuraSync(false);

        Assert.False(_lighting.IsAuraSyncEnabled);
    }

    #endregion

    #region Secondary Color Tests

    [Fact]
    public void SetSecondaryColor_StoresCorrectly()
    {
        _lighting.SetSecondaryColor(255, 128, 64);

        var (r, g, b) = _lighting.GetSecondaryColor();
        Assert.Equal(255, r);
        Assert.Equal(128, g);
        Assert.Equal(64, b);
    }

    [Fact]
    public void GetSecondaryColor_DefaultIsZero()
    {
        var (r, g, b) = _lighting.GetSecondaryColor();
        Assert.Equal(0, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }

    #endregion

    #region State Tracking Tests

    [Fact]
    public void InitialState_DefaultValues()
    {
        Assert.Equal(AuraMode.Static, _lighting.CurrentMode);
        Assert.Equal(AuraZone.Keyboard, _lighting.CurrentZone);
        Assert.Equal((0, (byte)0, (byte)0), _lighting.CurrentColor);
        Assert.Equal(0, _lighting.CurrentSpeed);
        Assert.False(_lighting.IsAuraSyncEnabled);
    }

    [Fact]
    public void SetMode_UpdatesState()
    {
        SetupWriterVerifiable();

        _lighting.SetMode(AuraMode.Breathe, AuraZone.Body, 100, 200, 50, 75, 1);

        Assert.Equal(AuraMode.Breathe, _lighting.CurrentMode);
        Assert.Equal(AuraZone.Body, _lighting.CurrentZone);
        Assert.Equal((100, (byte)200, (byte)50), _lighting.CurrentColor);
        Assert.Equal(75, _lighting.CurrentSpeed);
    }

    #endregion

    #region TurnOffAll Tests

    [Fact]
    public void TurnOffAll_SendsBlackToAllZones()
    {
        SetupWriterVerifiable();

        _lighting.TurnOffAll();

        _mockWriter.Verify(w => w.WriteReport(It.IsAny<IHidDeviceStream>(), It.IsAny<byte[]>()), Times.AtLeast(1));
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullAsusHid_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AuraLighting(null!));
    }

    [Fact]
    public void Constructor_WithValidHid_DoesNotThrow()
    {
        var writer = new Mock<IHidReportWriter>();
        var hid = new AsusHid(writer.Object);
        Assert.NotNull(new AuraLighting(hid));
    }

    #endregion

    #region PerKey Led Count Constant Tests

    [Fact]
    public void PerKeyLedCount_Is178()
    {
        Assert.Equal(178, AuraLighting.PerKeyLedCount);
    }

    [Fact]
    public void FourZoneCount_Is4()
    {
        Assert.Equal(4, AuraLighting.FourZoneCount);
    }

    #endregion
}