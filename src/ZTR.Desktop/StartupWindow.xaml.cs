using System.Windows;

namespace ZTR.Desktop;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
    }

    public void SetProgress(int percent, string status)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = Math.Clamp(percent, 0, 100);
            StatusText.Text = status;
        });
    }

    public void AppendLog(string message, bool isSuccess = false, bool isError = false)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = isSuccess ? "[OK]  " : isError ? "[ERR] " : "[LOAD]";
            var line = $"{timestamp} {prefix} {message}{Environment.NewLine}";
            LogOutput.AppendText(line);
            LogOutput.ScrollToEnd();
        });
    }
}
