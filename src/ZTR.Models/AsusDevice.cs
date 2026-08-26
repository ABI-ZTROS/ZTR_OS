namespace ZTR.Models;

// Performance modes
public enum AsusMode
{
    PerformanceSilent = 0,
    PerformanceBalanced = 1,
    PerformanceTurbo = 2,
    PerformanceFullSpeed = 3,
    PerformanceManual = 4
}

// Device IO control method IDs
public enum DeviceMethod : uint
{
    DSTS = 0x53545344,  // Read device status
    DEVS = 0x53564544,  // Write device status
    INIT = 0x54494E49,  // Initialize device
    WDOG = 0x474F4457   // Watchdog
}

// Device control IDs (from G-Helper)
public enum AsusDevice : uint
{
    PerformanceMode = 0x00050021,
    StatusMode = 0x00050012,
    CPUBacklight = 0x00050019,
    BatteryLimit = 0x00050024,
    BatteryDischarge = 0x00050025,
    KeyboardLight = 0x00050027,
    FnLock = 0x00050028,
    TouchpadToggle = 0x00050029,
    GPUEco = 0x00050051,
    GPUMux = 0x00050058,
    AudioMute = 0x0005002B,
    MicMute = 0x0005002C,
    CameraShutter = 0x0005002D,
    CameraLed = 0x0005002E,
    // Fan control
    CPU_Fan = 0x00050005,
    GPU_Fan = 0x00050006,
    Mid_Fan = 0x00050007,
    // Power limits
    PPT_APUA0 = 0x00050060,  // SPL
    PPT_APUA3 = 0x00050063,  // sPPT
    PPT_APUC1 = 0x00050071,  // fPPT
    // GPU modes
    GPUBase = 0x00050050,
    GPUPower = 0x00050052,
    // Screen
    ScreenOverdrive = 0x00050030,
    ScreenFHD = 0x00050031,
    ScreenMiniled1 = 0x00050032,
    ScreenMiniled2 = 0x00050033,
    ScreenOptimalBrightness = 0x00050034,
    // Charger
    ChargerMode = 0x00050026,
    // Devices
    DevsCPUFanCurve = 0x00050040,
    DevsGPUFanCurve = 0x00050041,
    DevsMidFanCurve = 0x00050042
}

public enum AsusFan
{
    CPU = 0,
    GPU = 1,
    Mid = 2,
    XGM = 3
}

public enum AsusGPU
{
    Eco = 0,
    Standard = 1,
    Ultimate = 2
}

public enum AuraMode
{
    Static = 0,
    Breathe = 1,
    ColorCycle = 2,
    Rainbow = 3,
    Star = 4,
    Rain = 5,
    Highlight = 6,
    Laser = 7,
    Ripple = 8,
    Strobe = 10,
    Comet = 11,
    Flash = 12,
    Heatmap = 20,
    GPUMode = 21,
    Ambient = 22,
    Battery = 23,
    Gradient = 24,
    ZoneTest = 25,
    Audio = 26,
    AudioPulse = 27
}

public enum AuraZone
{
    Keyboard = 0,
    Touchpad = 1,
    Body = 2,
    Rear = 3,
    Mouse = 4,
    Monitor = 5
}

/// <summary>
/// MiniLED display modes for ASUS OLED and MiniLED panels.
/// </summary>
public enum MiniLedMode
{
    Off = 0,
    Standard = 1,
    Advanced = 2
}

/// <summary>
/// Battery charger modes for ASUS devices.
/// </summary>
public enum ChargerMode
{
    ACOnly = 0,
    BatteryOnly = 1,
    Both = 2
}

/// <summary>
/// Keyboard backlight zones for per-zone control.
/// </summary>
public enum KeyboardZone
{
    Zone1 = 0,
    Zone2 = 1,
    Zone3 = 2,
    Zone4 = 3
}

/// <summary>
/// Controller input modes for ASUS Ally and similar handheld devices.
/// </summary>
public enum ControllerMode
{
    Auto = 0,
    Gamepad = 1,
    WASD = 2,
    Mouse = 3
}

/// <summary>
/// Screen refresh rate options for ASUS devices.
/// </summary>
public enum ScreenRefreshRate
{
    Hz60 = 60,
    Hz120 = 120,
    Hz144 = 144,
    Hz165 = 165,
    Hz240 = 240,
    Hz300 = 300
}