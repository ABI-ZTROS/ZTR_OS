namespace ZTR.Desktop.Features.WebView2.Services;

public interface IWebView2BridgeService
{
    event EventHandler<BridgeEventArgs>? EventReceived;
    Task<string?> InvokeAsync(string method, params object[] args);
    void RegisterHandler(string method, Func<object[], Task<object?>> handler);
    void UnregisterHandler(string method);
}

public class BridgeEventArgs : EventArgs
{
    public string Method { get; }
    public object?[] Args { get; }
    public BridgeEventArgs(string method, object?[] args) { Method = method; Args = args; }
}
