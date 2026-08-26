namespace ZTR.HAL;

/// <summary>
/// Custom effect calculators for Aura lighting.
/// Generates color arrays based on sensor data such as temperature, battery level,
/// audio input, and screen color sampling.
/// </summary>
public class AuraEffects
{
    /// <summary>
    /// Calculates a heatmap color based on CPU and GPU temperatures.
    /// Maps temperature ranges to color gradients: cool (blue) -> warm (green) -> hot (red).
    /// </summary>
    /// <param name="cpuTemp">CPU temperature in Celsius (0-100+).</param>
    /// <param name="gpuTemp">GPU temperature in Celsius (0-100+).</param>
    /// <returns>An (R, G, B) tuple representing the heatmap color.</returns>
    public (byte R, byte G, byte B) CalculateHeatmapColor(int cpuTemp, int gpuTemp)
    {
        int maxTemp = Math.Max(cpuTemp, gpuTemp);

        byte r, g, b;

        if (maxTemp >= 90)
        {
            r = 255;
            g = (byte)Math.Max(0, 255 - (maxTemp - 90) * 25);
            b = 0;
        }
        else if (maxTemp >= 75)
        {
            int t = maxTemp - 75;
            r = (byte)Math.Min(255, t * 17);
            g = 255;
            b = 0;
        }
        else if (maxTemp >= 60)
        {
            int t = maxTemp - 60;
            r = 0;
            g = 255;
            b = (byte)Math.Min(255, t * 17);
        }
        else if (maxTemp >= 45)
        {
            int t = maxTemp - 45;
            r = 0;
            g = (byte)Math.Max(100, 255 - t * 10);
            b = 255;
        }
        else
        {
            r = 0;
            g = 100;
            b = 255;
        }

        return (r, g, b);
    }

    /// <summary>
    /// Calculates an ambient color based on a sampled screen color.
    /// Applies brightness normalization and color enhancement.
    /// </summary>
    /// <param name="screenColor">The raw screen color as an (R, G, B) tuple.</param>
    /// <returns>An enhanced (R, G, B) tuple suitable for ambient lighting.</returns>
    public (byte R, byte G, byte B) CalculateAmbientColor((byte R, byte G, byte B) screenColor)
    {
        var (r, g, b) = screenColor;

        int maxComponent = Math.Max(r, Math.Max(g, b));
        int minComponent = Math.Min(r, Math.Min(g, b));
        int saturation = maxComponent == 0 ? 0 : (maxComponent - minComponent);

        float brightness = (r + g + b) / 3.0f;
        float enhancement = brightness < 64 ? 1.5f : brightness < 128 ? 1.2f : brightness < 192 ? 1.0f : 0.85f;

        byte rr = (byte)Math.Clamp((int)(r * enhancement + saturation * 0.3), 0, 255);
        byte gg = (byte)Math.Clamp((int)(g * enhancement + saturation * 0.2), 0, 255);
        byte bb = (byte)Math.Clamp((int)(b * enhancement + saturation * 0.1), 0, 255);

        return (rr, gg, bb);
    }

    /// <summary>
    /// Calculates a battery indicator color based on charge percentage.
    /// Maps charge levels: low (red) -> medium (yellow) -> high (green).
    /// </summary>
    /// <param name="chargePercent">Battery charge percentage (0-100).</param>
    /// <returns>An (R, G, B) tuple representing the battery level color.</returns>
    public (byte R, byte G, byte B) CalculateBatteryColor(int chargePercent)
    {
        chargePercent = Math.Clamp(chargePercent, 0, 100);

        byte r, g, b;

        if (chargePercent <= 20)
        {
            r = 255;
            g = (byte)(chargePercent * 12);
            b = 50;
        }
        else if (chargePercent <= 50)
        {
            float t = (chargePercent - 20f) / 30f;
            r = (byte)(255 * (1 - t));
            g = (byte)(240 * t + 120 * (1 - t));
            b = 80;
        }
        else if (chargePercent <= 80)
        {
            float t = (chargePercent - 50f) / 30f;
            r = 0;
            g = (byte)(200 + 55 * t);
            b = (byte)(100 - 50 * t);
        }
        else
        {
            r = 0;
            g = 255;
            b = 50;
        }

        return (r, g, b);
    }

    /// <summary>
    /// Calculates an audio-reactive color based on audio intensity level.
    /// Maps audio levels to vibrant color transitions:
    /// silence (dim blue) -> quiet (cyan) -> normal (green) -> loud (yellow) -> peak (red/white).
    /// </summary>
    /// <param name="audioLevel">Audio intensity level (0-255).</param>
    /// <returns>An (R, G, B) tuple representing the audio-reactive color.</returns>
    public (byte R, byte G, byte B) CalculateAudioColor(int audioLevel)
    {
        audioLevel = Math.Clamp(audioLevel, 0, 255);

        byte r, g, b;

        if (audioLevel <= 50)
        {
            float t = audioLevel / 50f;
            r = 0;
            g = (byte)(50 + 100 * t);
            b = (byte)(100 + 155 * t);
        }
        else if (audioLevel <= 120)
        {
            float t = (audioLevel - 50f) / 70f;
            r = 0;
            g = (byte)(150 + 105 * t);
            b = (byte)(255 * (1 - t));
        }
        else if (audioLevel <= 200)
        {
            float t = (audioLevel - 120f) / 80f;
            r = (byte)(255 * t);
            g = 255;
            b = 0;
        }
        else
        {
            float t = (audioLevel - 200f) / 55f;
            r = 255;
            g = (byte)(255 * (1 - t * 0.2f));
            b = (byte)(255 * t);
        }

        return (r, g, b);
    }

    /// <summary>
    /// Generates an array of heatmap colors for multi-zone layouts.
    /// </summary>
    /// <param name="cpuTemp">CPU temperature in Celsius.</param>
    /// <param name="gpuTemp">GPU temperature in Celsius.</param>
    /// <param name="zoneCount">Number of zones to generate colors for.</param>
    /// <returns>An array of (R, G, B) tuples, one per zone.</returns>
    public (byte R, byte G, byte B)[] GenerateHeatmapZones(int cpuTemp, int gpuTemp, int zoneCount)
    {
        var colors = new (byte R, byte G, byte B)[zoneCount];
        var baseColor = CalculateHeatmapColor(cpuTemp, gpuTemp);

        for (int i = 0; i < zoneCount; i++)
        {
            float factor = 0.6f + 0.4f * ((float)i / Math.Max(1, zoneCount - 1));
            colors[i] = (
                (byte)Math.Clamp((int)(baseColor.R * factor), 0, 255),
                (byte)Math.Clamp((int)(baseColor.G * factor), 0, 255),
                (byte)Math.Clamp((int)(baseColor.B * factor), 0, 255)
            );
        }

        return colors;
    }

    /// <summary>
    /// Generates an array of battery-based colors for multi-zone layouts.
    /// </summary>
    /// <param name="chargePercent">Battery charge percentage (0-100).</param>
    /// <param name="zoneCount">Number of zones to generate colors for.</param>
    /// <returns>An array of (R, G, B) tuples, one per zone.</returns>
    public (byte R, byte G, byte B)[] GenerateBatteryZones(int chargePercent, int zoneCount)
    {
        var colors = new (byte R, byte G, byte B)[zoneCount];
        var baseColor = CalculateBatteryColor(chargePercent);

        for (int i = 0; i < zoneCount; i++)
        {
            float factor = 0.5f + 0.5f * ((float)i / Math.Max(1, zoneCount - 1));
            colors[i] = (
                (byte)Math.Clamp((int)(baseColor.R * factor), 0, 255),
                (byte)Math.Clamp((int)(baseColor.G * factor), 0, 255),
                (byte)Math.Clamp((int)(baseColor.B * factor), 0, 255)
            );
        }

        return colors;
    }

    /// <summary>
    /// Generates an array of audio-reactive colors for multi-zone layouts.
    /// </summary>
    /// <param name="audioLevels">Array of audio intensity levels (0-255 per band).</param>
    /// <param name="zoneCount">Number of zones to generate colors for.</param>
    /// <returns>An array of (R, G, B) tuples, one per zone.</returns>
    public (byte R, byte G, byte B)[] GenerateAudioZones(int[] audioLevels, int zoneCount)
    {
        var colors = new (byte R, byte G, byte B)[zoneCount];

        for (int i = 0; i < zoneCount; i++)
        {
            int level = i < audioLevels.Length ? audioLevels[i] : 0;
            colors[i] = CalculateAudioColor(level);
        }

        return colors;
    }

    /// <summary>
    /// Generates an array of ambient colors for multi-zone layouts from screen sampling.
    /// </summary>
    /// <param name="screenColors">Array of screen color samples.</param>
    /// <param name="zoneCount">Number of zones to generate colors for.</param>
    /// <returns>An array of (R, G, B) tuples, one per zone.</returns>
    public (byte R, byte G, byte B)[] GenerateAmbientZones((byte R, byte G, byte B)[] screenColors, int zoneCount)
    {
        var colors = new (byte R, byte G, byte B)[zoneCount];

        for (int i = 0; i < zoneCount; i++)
        {
            var screenColor = i < screenColors.Length ? screenColors[i] : (R: (byte)0, G: (byte)0, B: (byte)0);
            colors[i] = CalculateAmbientColor(screenColor);
        }

        return colors;
    }
}