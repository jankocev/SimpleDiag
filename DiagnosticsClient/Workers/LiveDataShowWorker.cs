using DiagnosticsClient.Entity;
using DiagnosticsClient.Service;
using Spectre.Console;

namespace DiagnosticsClient.Workers;

public class LiveDataShowWorker
{
    private List<ServerState> ServerStates = new();
    private DataService _dataService;
    private ServerService _serverService;

    public LiveDataShowWorker(DataService dataService, ServerService serverService)
    {
        _dataService = dataService;
        _serverService = serverService;
    }

    public async Task ExecuteAsync(CancellationToken cancelToken)
    {
        await AnsiConsole
            .Live(new Table())
            .StartAsync(async ctx =>
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(await RenderTable(cancelToken));

                    await Task.Delay(TimeSpan.FromSeconds(10), cancelToken);
                }
            });
    }

    private async Task<Table> RenderTable(CancellationToken cancelToken)
    {
        var servers = await _serverService.LoadServersAsync();
        foreach (var server in servers)
        {
            await _dataService.FetchData(server, cancelToken);
        }

        await _dataService.CleanOldRecordsAsync();

        var data = await _dataService.LoadServerStatesAsync();

        var table = new Table();
        table.AddColumn("Status");
        table.AddColumn("Name");
        table.AddColumn("Ram");
        table.AddColumn("Disk");
        table.AddColumn("Docker");

        foreach (var stat in data)
        {
            var color = stat.IsOk ? "white" : "red";
            var status = stat.IsOk ? "" : "";
            table.AddRow(
                $"[{color}]{status}[/]",
                $"[{color}]{stat.Server!.Name}[/]",
                $"[{color}]{stat.Ram}%[/]",
                $"[{color}]{stat.Disk}%[/]",
                $"[{color}]{stat.DockerServiceCount}[/]"
            );
        }
        return table;
    }
}
