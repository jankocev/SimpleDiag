using System.Diagnostics;
using System.Text.Json;
using DiagnosticsClient.Entity;
using DiagnosticsClient.Service;
using Microsoft.Extensions.Hosting;

namespace DiagnosticsClient.Workers;

public class DataFetchWorker : BackgroundService
{
    private readonly DataService _dataService;
    private readonly ServerService _serverService;
    private readonly HttpClient _http = new();

    public DataFetchWorker(DataService dataService, ServerService serverService)
    {
        _dataService = dataService;
        _serverService = serverService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<ServerEntity> servers = await _serverService.LoadServersAsync();
            Debug.WriteLine("Servers loaded:" + servers.Count());
            foreach (var server in servers)
            {
                await _dataService.FetchData(server, stoppingToken);
            }
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            await CheckStorage();
        }
    }

    public async Task CheckStorage()
    {
        await _dataService.CleanOldRecordsAsync();
    }
}
