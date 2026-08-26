using Microsoft.AspNetCore.SignalR;
using ZTR.Models;

namespace ZTR.Api.Hubs;

public class SensorHub : Hub
{
    private readonly ILogger<SensorHub> _logger;

    public SensorHub(ILogger<SensorHub> logger)
    {
        _logger = logger;
    }

    public async Task SendSensorState(HardwareState state)
    {
        await Clients.All.SendAsync("SensorUpdate", state);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SensorHub client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SensorHub client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}