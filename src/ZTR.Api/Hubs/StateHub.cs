using Microsoft.AspNetCore.SignalR;

namespace ZTR.Api.Hubs;

public class StateHub : Hub
{
    private readonly ILogger<StateHub> _logger;

    public StateHub(ILogger<StateHub> logger)
    {
        _logger = logger;
    }

    public async Task SendStateChange(string type, object data)
    {
        await Clients.All.SendAsync("StateChange", type, data);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("StateHub client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("StateHub client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}