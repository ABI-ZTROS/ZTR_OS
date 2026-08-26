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

// Device control IDs (verified against G-Helper's AsusACPI.cs)
public enum AsusDevice : uint
{
    PerformanceMode = 0x00120075,
    StatusMode = 0x00090031,
    PowerSavingMode = 0x00090032,
    CPUBacklight = 0x00050019,
    BatteryLimit = 0x00120057,
    BatteryDischarge = 0x0012005A,
    KeyboardLight = 0x00050027,
    FnLock = 0x00100023,
    TouchpadToggle = 0x00050029,
    GPUEco = 0x00090020,       // GPUEcoROG
    GPUMux = 0x00090016,       // GPUMuxROG
    AudioMute = 0x0005002B,
    MicMute = 0x0005002C,
    CameraShutter = 0x0005002D,
    CameraLed = 0x0005002E,
    // Fan control
    CPU_Fan = 0x00110013,
    GPU_Fan = 0x00110014,
    Mid_Fan = 0x00110031,
    // Temperature sensors (separate from fan IDs)
    Temp_CPU = 0x00120094,
    Temp_GPU = 0x00120097,
    // Power limits (PPT - Platform Power Tracking)
    PPT_APUA0 = 0x001200A0,  // SPL (slow boost limit) / PL2
    PPT_APUA3 = 0x001200A3,  // sPPT
    PPT_APUC1 = 0x001200C1,  // fPPT (fast boost limit)
    PPT_APUC2 = 0x001200C2,  // GPU Temp Target
    PPT_EDCA1 = 0x001200A1,  // CPU EDC
    PPT_TDCA2 = 0x001200A2,  // CPU TDC
    PPT_CPUB0 = 0x001200B0,  // CPU PPT on 2022
    PPT_CPUB1 = 0x001200B1,  // Total PPT on 2022
    PPT_GPUC0 = 0x001200C0,  // NVIDIA GPU Boost
    PPT_GPUC2 = 0x001200C2,  // NVIDIA GPU Temp Target
    PPT_GPUCPU9C = 0x0012009C,  // GPU to CPU Dynamic Boost
    PPT_TEMP9E = 0x0012009E,   // CPU Temperature Limit
    PPT_CROSS9F = 0x0012009F,  // Cross Loading Processor Power
    // GPU modes
    GPUBase = 0x00120099,
    GPUPower = 0x00120098,
    // Screen
    ScreenOverdrive = 0x00050019,
    ScreenFHD = 0x00050031,
    ScreenMiniled1 = 0x00050032,
    ScreenMiniled2 = 0x00050033,
    ScreenOptimalBrightness = 0x00050034,
    // Charger
    ChargerMode = 0x0012006C,
    // Fan curves
    DevsCPUFanCurve = 0x00110024,
    DevsGPUFanCurve = 0x00110025,
    DevsMidFanCurve = 0x00110032,
    // CPU core config
    CORES_CPU = 0x001200D2,
    CORES_MAX = 0x001200D3,
    CORES_MIN = 0x001200D4
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
