namespace ZTR.Desktop.Features.WebView2.Services;

public interface IWebView2BridgeService
{
    event EventHandler<BridgeEventArgs>? EventReceived;
    
    /// <summary>
    /// Attaches the bridge to a WebView2 instance, enabling JS↔C# communication.
    /// </summary>
    void Attach(Microsoft.Web.WebView2.Core.CoreWebView2 webView);
    
    /// <summary>
    /// Detaches the bridge from the current WebView2 instance.
    /// </summary>
    void Detach();
    
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
