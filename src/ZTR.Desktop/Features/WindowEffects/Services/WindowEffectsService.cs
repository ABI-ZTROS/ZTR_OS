using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZTR.Desktop.Features.WindowEffects.Services;

public class WindowEffectsService : IWindowEffectsService
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    [StructLayout(LayoutKind.Sequential)]
    private struct DWM_WINDOW_CORNER_PREFERENCE { }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public void ApplyMica(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int mica = 3;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref mica, sizeof(int));
            ForceLog.Write($"[WINFX] Mica 效果已应用: {window.Title ?? window.Name}");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[WINFX] ApplyMica 失败: {ex.Message}");
        }
    }

    public void ApplyDarkTitleBar(Window window, bool dark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch { }
    }

    public void ApplyRoundedCorners(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int rounded = 2;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref rounded, sizeof(int));
        }
        catch { }
    }

    public void RemoveMica(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            int none = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref none, sizeof(int));
        }
        catch { }
    }
}
