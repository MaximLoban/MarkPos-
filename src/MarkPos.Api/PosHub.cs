using Microsoft.AspNetCore.SignalR;
using MarkPos.Application.Session;

namespace MarkPos.Api;

public class PosHub : Hub { }

public class PosStateNotifier : IHostedService
{
    private readonly IPosSession _session;
    private readonly IHubContext<PosHub> _hub;

    public PosStateNotifier(IPosSession session, IHubContext<PosHub> hub)
    {
        _session = session;
        _hub = hub;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _session.StateChanged += OnStateChanged;
        Console.WriteLine("Notifier subscribed");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _session.StateChanged -= OnStateChanged;
        return Task.CompletedTask;
    }

    private void OnStateChanged(PosState state)
    {
        Console.WriteLine("NOTIFIER TRIGGERED");

        _ = _hub.Clients.All.SendAsync("StateChanged", state);
    }
}