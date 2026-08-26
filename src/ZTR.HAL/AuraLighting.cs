using ZTR.Models;

namespace ZTR.HAL;

/// <summary>
/// Manages Aura RGB lighting for ASUS devices.
/// Supports all lighting modes from G-Helper including custom effects,
/// PerKey (178 LED) direct mode, 4-zone layouts, and Aura sync.
/// </summary>
public class AuraLighting
{
    private readonly AsusHid _hid;
    private readonly AuraEffects _effects;

    private AuraMode _currentMode = AuraMode.Static;
    private AuraZone _currentZone = AuraZone.Keyboard;
    private byte _red, _green, _blue;
    private int _speed;
    private int _direction;
    private byte _random;
    private byte _red2, _green2, _blue2;
    private bool _auraSync;
    private int _brightness = 80;
    private readonly Dictionary<AuraZone, AuraMode> _zoneModes = new();

    /// <summary>
    /// Number of LEDs in PerKey direct mode layout.
    /// </summary>
    public const int PerKeyLedCount = 178;

    /// <summary>
    /// Number of zones in 4-zone layout.
    /// </summary>
    public const int FourZoneCount = 4;

    /// <summary>
    /// The maximum number of regions that can be independently controlled.
    /// </summary>
    public const int MaxRegions = 4;

    /// <summary>
    /// Initializes a new instance of the AuraLighting class.
    /// </summary>
    /// <param name="hid">The AsusHid instance for communication.</param>
    public AuraLighting(AsusHid hid)
    {
        _hid = hid ?? throw new ArgumentNullException(nameof(hid));
        _effects = new AuraEffects();
    }

    /// <summary>
    /// Gets or sets the current Aura mode.
    /// </summary>
    public AuraMode CurrentMode => _currentMode;

    /// <summary>
    /// Gets or sets the current Aura zone.
    /// </summary>
    public AuraZone CurrentZone => _currentZone;

    /// <summary>
    /// Gets the current primary color.
    /// </summary>
    public (byte R, byte G, byte B) CurrentColor => (_red, _green, _blue);

    /// <summary>
    /// Gets the current speed setting.
    /// </summary>
    public int CurrentSpeed => _speed;

    /// <summary>
    /// Gets whether Aura sync is enabled.
    /// </summary>
    public bool IsAuraSyncEnabled => _auraSync;

    /// <summary>
    /// Sets a static color for the specified zone.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetStatic(AuraZone zone, byte r, byte g, byte b)
    {
        _currentMode = AuraMode.Static;
        _currentZone = zone;
        (_red, _green, _blue) = (r, g, b);

        return ApplyMessage(AuraMessage(AuraMode.Static, zone, r, g, b, 0, 0));
    }

    /// <summary>
    /// Sets a breathing effect with the specified color and speed.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Breathing speed (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetBreathe(AuraZone zone, byte r, byte g, byte b, int speed)
    {
        return SetMode(AuraMode.Breathe, zone, r, g, b, speed, 0);
    }

    /// <summary>
    /// Sets a color cycle effect with the specified speed.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="speed">Cycle speed (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetColorCycle(AuraZone zone, int speed)
    {
        return SetMode(AuraMode.ColorCycle, zone, 0, 0, 0, speed, 0);
    }

    /// <summary>
    /// Sets a rainbow effect with the specified speed and direction.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="speed">Rainbow speed (0-255).</param>
    /// <param name="direction">Direction (0=right, 1=left).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetRainbow(AuraZone zone, int speed, int direction)
    {
        return SetMode(AuraMode.Rainbow, zone, 0, 0, 0, speed, direction);
    }

    /// <summary>
    /// Sets a strobing effect with the specified color and speed.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Strobe speed (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetStrobe(AuraZone zone, byte r, byte g, byte b, int speed)
    {
        return SetMode(AuraMode.Strobe, zone, r, g, b, speed, 0);
    }

    /// <summary>
    /// Sets a starry effect (PerKey) with the specified color, speed, and randomness.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetStar(AuraZone zone, byte r, byte g, byte b, int speed, byte random)
    {
        return SetMode(AuraMode.Star, zone, r, g, b, speed, 0, random);
    }

    /// <summary>
    /// Sets a rain effect (PerKey) with the specified color, speed, and randomness.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetRain(AuraZone zone, byte r, byte g, byte b, int speed, byte random)
    {
        return SetMode(AuraMode.Rain, zone, r, g, b, speed, 0, random);
    }

    /// <summary>
    /// Sets a highlight effect (PerKey) with the specified color and speed.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetHighlight(AuraZone zone, byte r, byte g, byte b, int speed)
    {
        return SetMode(AuraMode.Highlight, zone, r, g, b, speed, 0);
    }

    /// <summary>
    /// Sets a laser effect (PerKey) with the specified color, speed, and direction.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Laser direction (0-3).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetLaser(AuraZone zone, byte r, byte g, byte b, int speed, int direction)
    {
        return SetMode(AuraMode.Laser, zone, r, g, b, speed, direction);
    }

    /// <summary>
    /// Sets a ripple effect (PerKey) with the specified color, speed, and randomness.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetRipple(AuraZone zone, byte r, byte g, byte b, int speed, byte random)
    {
        return SetMode(AuraMode.Ripple, zone, r, g, b, speed, 0, random);
    }

    /// <summary>
    /// Sets a comet effect (PerKey) with the specified color, speed, and direction.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Comet direction (0-3).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetComet(AuraZone zone, byte r, byte g, byte b, int speed, int direction)
    {
        return SetMode(AuraMode.Comet, zone, r, g, b, speed, direction);
    }

    /// <summary>
    /// Sets a flash effect (PerKey) with the specified color, speed, and randomness.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetFlash(AuraZone zone, byte r, byte g, byte b, int speed, byte random)
    {
        return SetMode(AuraMode.Flash, zone, r, g, b, speed, 0, random);
    }

    /// <summary>
    /// Sets a gradient effect with the specified color, speed, and direction.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Gradient direction (0-3).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetGradient(AuraZone zone, byte r, byte g, byte b, int speed, int direction)
    {
        return SetMode(AuraMode.Gradient, zone, r, g, b, speed, direction);
    }

    /// <summary>
    /// Sets a GPU-linked lighting mode.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetGPUMode(AuraZone zone)
    {
        return SetMode(AuraMode.GPUMode, zone, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Sets a zone test mode for debugging.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetZoneTest(AuraZone zone, byte r, byte g, byte b)
    {
        return SetMode(AuraMode.ZoneTest, zone, r, g, b, 0, 0);
    }

    /// <summary>
    /// Sets heatmap mode (CPU/GPU temperature mapped to color).
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="cpuTemp">CPU temperature in Celsius.</param>
    /// <param name="gpuTemp">GPU temperature in Celsius.</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetHeatmap(AuraZone zone, int cpuTemp, int gpuTemp)
    {
        var (r, g, b) = _effects.CalculateHeatmapColor(cpuTemp, gpuTemp);
        return SetMode(AuraMode.Heatmap, zone, r, g, b, 0, 0);
    }

    /// <summary>
    /// Sets ambient mode (screen color sampled and mapped to lighting).
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="screenColor">The sampled screen color (RGB).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetAmbient(AuraZone zone, (byte R, byte G, byte B) screenColor)
    {
        var (r, g, b) = _effects.CalculateAmbientColor(screenColor);
        return SetMode(AuraMode.Ambient, zone, r, g, b, 0, 0);
    }

    /// <summary>
    /// Sets battery mode (charge level mapped to color).
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="chargePercent">Battery charge percentage (0-100).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetBattery(AuraZone zone, int chargePercent)
    {
        var (r, g, b) = _effects.CalculateBatteryColor(chargePercent);
        return SetMode(AuraMode.Battery, zone, r, g, b, 0, 0);
    }

    /// <summary>
    /// Sets audio reactive mode with spectrum visualization.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="audioLevel">Audio intensity level (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetAudio(AuraZone zone, int audioLevel)
    {
        var (r, g, b) = _effects.CalculateAudioColor(audioLevel);
        return SetMode(AuraMode.Audio, zone, r, g, b, 0, 0);
    }

    /// <summary>
    /// Sets audio pulse mode.
    /// </summary>
    /// <param name="zone">The Aura zone to set.</param>
    /// <param name="audioLevel">Audio intensity level (0-255).</param>
    /// <param name="speed">Pulse speed (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetAudioPulse(AuraZone zone, int audioLevel, int speed)
    {
        var (r, g, b) = _effects.CalculateAudioColor(audioLevel);
        return SetMode(AuraMode.AudioPulse, zone, r, g, b, speed, 0);
    }

    /// <summary>
    /// Set a lighting mode with full parameter control.
    /// </summary>
    /// <param name="mode">The Aura mode to set.</param>
    /// <param name="zone">The Aura zone to configure.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Effect direction (0-3).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetMode(AuraMode mode, AuraZone zone, byte r, byte g, byte b, int speed = 0, int direction = 0, byte random = 0)
    {
        _currentMode = mode;
        _currentZone = zone;
        _zoneModes[zone] = mode;
        (_red, _green, _blue) = (r, g, b);
        _speed = speed;
        _direction = direction;
        _random = random;
        (_red2, _green2, _blue2) = (r, g, b);

        return ApplyMessage(AuraMessage(mode, zone, r, g, b, speed, direction, random, r, g, b));
    }

    /// <summary>
    /// Sets direct per-LED mode for 178-LED keyboards (PerKey layout).
    /// Each LED is individually addressed with its own color.
    /// </summary>
    /// <param name="ledColors">Array of RGB values for each LED (length must be 178 * 3 = 534 bytes).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetDirectMode(byte[] ledColors)
    {
        if (ledColors == null)
            throw new ArgumentNullException(nameof(ledColors));

        if (ledColors.Length != PerKeyLedCount * 3)
            throw new ArgumentException($"Expected {PerKeyLedCount * 3} bytes for {PerKeyLedCount} LEDs, got {ledColors.Length}", nameof(ledColors));

        return _hid.SetFeatureAura(ledColors);
    }

    /// <summary>
    /// Sets direct 4-zone mode with individual zone colors.
    /// </summary>
    /// <param name="zoneColors">Array of 4 RGB color tuples, one per zone.</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetDirectMode4Zone((byte R, byte G, byte B)[] zoneColors)
    {
        if (zoneColors == null)
            throw new ArgumentNullException(nameof(zoneColors));

        if (zoneColors.Length != FourZoneCount)
            throw new ArgumentException($"Expected {FourZoneCount} zone colors, got {zoneColors.Length}", nameof(zoneColors));

        bool allSuccess = true;
        for (int i = 0; i < FourZoneCount; i++)
        {
            var (r, g, b) = zoneColors[i];
            var zone = (AuraZone)i;
            allSuccess = SetStatic(zone, r, g, b) && allSuccess;
        }
        return allSuccess;
    }

    /// <summary>
    /// Enables or disables Aura sync between mouse and keyboard.
    /// </summary>
    /// <param name="enabled">True to enable Aura sync, false to disable.</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetAuraSync(bool enabled)
    {
        _auraSync = enabled;

        if (enabled)
        {
            byte[] syncData = { 0x5D, 0xB6, 0x01, 0x00, 0x00 };
            return _hid.Write(syncData, "AuraSync Enable");
        }
        else
        {
            byte[] syncData = { 0x5D, 0xB6, 0x00, 0x00, 0x00 };
            return _hid.Write(syncData, "AuraSync Disable");
        }
    }

    /// <summary>
    /// Sets the secondary color for effects that use two colors.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    public void SetSecondaryColor(byte r, byte g, byte b)
    {
        _red2 = r;
        _green2 = g;
        _blue2 = b;
    }

    /// <summary>
    /// Gets the secondary color for two-color effects.
    /// </summary>
    /// <returns>The secondary color as an (R, G, B) tuple.</returns>
    public (byte R, byte G, byte B) GetSecondaryColor() => (_red2, _green2, _blue2);

    /// <summary>
    /// Applies a region-based lighting configuration.
    /// </summary>
    /// <param name="regionIndex">The region index (0-3).</param>
    /// <param name="mode">The lighting mode for this region.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Effect direction (0-3).</param>
    /// <returns>True if the command was sent successfully.</returns>
    public bool SetRegion(int regionIndex, AuraMode mode, byte r, byte g, byte b, int speed = 0, int direction = 0)
    {
        if (regionIndex < 0 || regionIndex >= MaxRegions)
            throw new ArgumentOutOfRangeException(nameof(regionIndex), $"Region must be between 0 and {MaxRegions - 1}");

        var zone = (AuraZone)regionIndex;
        return SetMode(mode, zone, r, g, b, speed, direction);
    }

    /// <summary>
    /// Gets the current mode for a specific zone.
    /// </summary>
    /// <param name="zone">The Aura zone to query.</param>
    /// <returns>The current mode for the zone.</returns>
    public AuraMode GetCurrentMode(AuraZone zone)
    {
        return _zoneModes.TryGetValue(zone, out var mode) ? mode : _currentMode;
    }

    /// <summary>
    /// Sets the brightness level for all zones.
    /// </summary>
    /// <param name="brightness">Brightness level (0-100).</param>
    public void SetBrightness(int brightness)
    {
        _brightness = Math.Clamp(brightness, 0, 100);
    }

    /// <summary>
    /// Gets the current brightness level.
    /// </summary>
    /// <returns>The brightness level (0-100).</returns>
    public int GetBrightness() => _brightness;

    /// <summary>
    /// Turns off all Aura lighting by setting static black on all zones.
    /// </summary>
    /// <returns>True if the command was sent successfully.</returns>
    public bool TurnOffAll()
    {
        bool allSuccess = true;
        foreach (AuraZone zone in Enum.GetValues<AuraZone>())
        {
            if (zone <= AuraZone.Mouse)
            {
                allSuccess = SetStatic(zone, 0, 0, 0) && allSuccess;
            }
        }
        return allSuccess;
    }

    /// <summary>
    /// Builds an Aura message in the 17-byte format matching G-Helper's AuraMessage structure.
    /// Format: [0x5D, 0xB3, Zone, Mode, R, G, B, Speed, Direction, Random, R2, G2, B2, 0x00, 0x00, 0x00, 0x00]
    /// </summary>
    /// <param name="mode">The Aura lighting mode.</param>
    /// <param name="zone">The Aura zone.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="speed">Effect speed (0-255).</param>
    /// <param name="direction">Effect direction (0-3).</param>
    /// <param name="random">Randomness factor (0-255).</param>
    /// <param name="r2">Secondary red component (0-255).</param>
    /// <param name="g2">Secondary green component (0-255).</param>
    /// <param name="b2">Secondary blue component (0-255).</param>
    /// <returns>A 17-byte array representing the Aura message.</returns>
    public static byte[] AuraMessage(AuraMode mode, AuraZone zone, byte r, byte g, byte b, int speed, int direction, byte random = 0, byte r2 = 0, byte g2 = 0, byte b2 = 0)
    {
        byte[] message = new byte[17];
        message[0] = 0x5D;
        message[1] = 0xB3;
        message[2] = (byte)zone;
        message[3] = (byte)mode;
        message[4] = r;
        message[5] = g;
        message[6] = b;
        message[7] = (byte)speed;
        message[8] = (byte)direction;
        message[9] = random;
        message[10] = r2;
        message[11] = g2;
        message[12] = b2;
        message[13] = 0x00;
        message[14] = 0x00;
        message[15] = 0x00;
        message[16] = 0x00;
        return message;
    }

    /// <summary>
    /// Applies a sequence of messages by sending data, SET, and APPLY commands.
    /// </summary>
    /// <param name="messages">The messages to send in sequence.</param>
    /// <returns>True if all commands were sent successfully.</returns>
    private bool ApplyMessage(params byte[][] messages)
    {
        var messageList = new List<byte[]>();

        foreach (var msg in messages)
        {
            messageList.Add(msg);
        }

        messageList.Add(new byte[] { 0x5D, 0xB5, 0x00, 0x00, 0x00 });
        messageList.Add(new byte[] { 0x5D, 0xB4 });

        return _hid.WriteBatch(messageList, "Aura", AsusHid.MainAuraPids.ToArray());
    }
}