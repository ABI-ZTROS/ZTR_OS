using System.Runtime.InteropServices;

namespace ZTR.HAL;

public static class SystemBatteryInfo
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved1;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    public static bool IsOnAcPower()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
                return status.ACLineStatus == 1;
        }
        catch { }
        return false;
    }

    public static int GetChargePercent()
    {
        try
        {
            if (GetSystemPowerStatus(out var status) && status.BatteryLifePercent <= 100)
                return status.BatteryLifePercent;
        }
        catch { }
        return -1;
    }

    public static bool IsCharging()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
                return status.ACLineStatus == 1 && status.BatteryFlag != 8;
        }
        catch { }
        return false;
    }
}