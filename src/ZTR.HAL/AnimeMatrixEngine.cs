using Microsoft.Extensions.Logging;
using ZTR.Models;

namespace ZTR.HAL;

public class AnimeMatrixEngine : IDisposable
{
    private readonly AsusHid _hid;
    private readonly AuraLighting _aura;
    private readonly ILogger<AnimeMatrixEngine>? _logger;
    private readonly System.Timers.Timer _animationTimer;
    private AnimationMode _currentMode;
    private bool _isRunning;
    private int _frameIndex;
    private bool _disposed;

    public enum AnimationMode
    {
        Slash,
        Pulse,
        Bounce,
        Clock,
        AudioSpectrum,
        CustomGif
    }

    public AnimeMatrixEngine(AsusHid hid, AuraLighting aura, ILogger<AnimeMatrixEngine>? logger = null)
    {
        _hid = hid;
        _aura = aura;
        _logger = logger;
        _animationTimer = new System.Timers.Timer(50);
        _animationTimer.Elapsed += OnAnimationTick;
        _animationTimer.AutoReset = true;
    }

    public void Start(AnimationMode mode)
    {
        if (_isRunning) Stop();
        _currentMode = mode;
        _frameIndex = 0;
        _isRunning = true;
        _animationTimer.Start();
        _logger?.LogInformation("Anime Matrix started: {Mode}", mode);
    }

    public void Stop()
    {
        _animationTimer.Stop();
        _isRunning = false;
        _logger?.LogInformation("Anime Matrix stopped");
    }

    public void SetSlash(int speed = 5)
    {
        Start(AnimationMode.Slash);
        _animationTimer.Interval = Math.Max(20, 200 - speed * 20);
    }

    public void SetPulse(byte r, byte g, byte b, int speed = 5)
    {
        Start(AnimationMode.Pulse);
        _animationTimer.Interval = Math.Max(20, 200 - speed * 20);
    }

    public void SetBounce(byte r, byte g, byte b, int speed = 5)
    {
        Start(AnimationMode.Bounce);
        _animationTimer.Interval = Math.Max(20, 200 - speed * 20);
    }

    public void SetClock()
    {
        Start(AnimationMode.Clock);
        _animationTimer.Interval = 1000;
    }

    public void SetAudioSpectrum(int speed = 5)
    {
        Start(AnimationMode.AudioSpectrum);
        _animationTimer.Interval = Math.Max(10, 100 - speed * 10);
    }

    public bool SetCustomFrame(byte[] ledColors)
    {
        return _aura.SetDirectMode(ledColors);
    }

    private void OnAnimationTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            _frameIndex++;
            byte[] frame = _currentMode switch
            {
                AnimationMode.Slash => GenerateSlashFrame(),
                AnimationMode.Pulse => GeneratePulseFrame(),
                AnimationMode.Bounce => GenerateBounceFrame(),
                AnimationMode.Clock => GenerateClockFrame(),
                AnimationMode.AudioSpectrum => GenerateAudioFrame(),
                _ => GenerateSlashFrame()
            };
            _aura.SetDirectMode(frame);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Anime Matrix frame generation failed");
        }
    }

    private byte[] GenerateSlashFrame()
    {
        int ledCount = AuraLighting.PerKeyLedCount;
        byte[] frame = new byte[ledCount * 3];
        int position = _frameIndex % ledCount;
        for (int i = 0; i < ledCount; i++)
        {
            int idx = i * 3;
            int dist = Math.Abs(i - position);
            if (dist < 3)
            {
                byte intensity = (byte)(255 - dist * 80);
                frame[idx] = intensity;
                frame[idx + 1] = 0;
                frame[idx + 2] = 0;
            }
        }
        return frame;
    }

    private byte[] GeneratePulseFrame()
    {
        int ledCount = AuraLighting.PerKeyLedCount;
        byte[] frame = new byte[ledCount * 3];
        double phase = (_frameIndex % 60) / 60.0;
        byte intensity = (byte)(Math.Sin(phase * Math.PI * 2) * 127 + 128);
        for (int i = 0; i < ledCount; i++)
        {
            int idx = i * 3;
            frame[idx] = 0;
            frame[idx + 1] = intensity;
            frame[idx + 2] = intensity;
        }
        return frame;
    }

    private byte[] GenerateBounceFrame()
    {
        int ledCount = AuraLighting.PerKeyLedCount;
        byte[] frame = new byte[ledCount * 3];
        int pos = Math.Abs(_frameIndex % (ledCount * 2) - ledCount);
        for (int i = 0; i < ledCount; i++)
        {
            int idx = i * 3;
            int dist = Math.Abs(i - pos);
            if (dist <= 2)
            {
                byte intensity = (byte)(255 - dist * 100);
                frame[idx] = 0;
                frame[idx + 1] = 0;
                frame[idx + 2] = intensity;
            }
        }
        return frame;
    }

    private byte[] GenerateClockFrame()
    {
        int ledCount = AuraLighting.PerKeyLedCount;
        byte[] frame = new byte[ledCount * 3];
        var now = DateTime.Now;
        int hour = now.Hour % 12;
        int minute = now.Minute;
        int second = now.Second;
        SetLed(frame, hour * 5, 255, 0, 0);
        SetLed(frame, minute, 0, 255, 0);
        SetLed(frame, second, 0, 0, 255);
        return frame;
    }

    private byte[] GenerateAudioFrame()
    {
        int ledCount = AuraLighting.PerKeyLedCount;
        byte[] frame = new byte[ledCount * 3];
        Random rnd = new Random(_frameIndex);
        for (int i = 0; i < ledCount; i++)
        {
            int idx = i * 3;
            byte level = (byte)rnd.Next(0, 256);
            frame[idx] = level;
            frame[idx + 1] = (byte)(255 - level);
            frame[idx + 2] = 128;
        }
        return frame;
    }

    private static void SetLed(byte[] frame, int index, byte r, byte g, byte b)
    {
        if (index * 3 + 2 < frame.Length)
        {
            frame[index * 3] = r;
            frame[index * 3 + 1] = g;
            frame[index * 3 + 2] = b;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _animationTimer.Dispose();
            _disposed = true;
        }
    }
}
