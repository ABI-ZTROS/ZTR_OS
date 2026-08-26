using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace ZTR.Desktop.Features.WebView2.Services;

public class WebView2BridgeService : IWebView2BridgeService
{
    private CoreWebView2? _webView;
    private readonly Dictionary<string, Func<object[], Task<object?>>> _handlers = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public event EventHandler<BridgeEventArgs>? EventReceived;

    public void Attach(CoreWebView2 webView)
    {
        _webView = webView;
        webView.WebMessageReceived += OnWebMessageReceived;
    }

    public void Detach()
    {
        if (_webView != null)
        {
            _webView.WebMessageReceived -= OnWebMessageReceived;
            _webView = null;
        }
    }

    public async Task<string?> InvokeAsync(string method, params object[] args)
    {
        if (_webView == null) return null;
        var payload = JsonSerializer.Serialize(new { method, args });
        try
        {
            await _webView.ExecuteScriptAsync($"window.__onbridgeevent?.({payload})");
            return payload;
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[BRIDGE] InvokeAsync 失败: {ex.Message}");
            return null;
        }
    }

    public void RegisterHandler(string method, Func<object[], Task<object?>> handler)
    {
        _handlers[method] = handler;
    }

    public void UnregisterHandler(string method)
    {
        _handlers.Remove(method);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("method", out var methodEl) && root.TryGetProperty("args", out var argsEl))
            {
                var method = methodEl.GetString()!;
                var args = argsEl.EnumerateArray().Select(a => (object?)a.GetString()).ToArray();
                EventReceived?.Invoke(this, new BridgeEventArgs(method, args));

                if (_handlers.TryGetValue(method, out var handler))
                {
                    var result = await handler(args);
                    if (result != null && _webView != null)
                    {
                        var response = JsonSerializer.Serialize(new { method = $"{method}_response", data = result });
                        await _webView.ExecuteScriptAsync($"window.__onbridgeevent?.({response})");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[BRIDGE] WebMessageReceived 处理失败: {ex.Message}");
        }
    }
}
