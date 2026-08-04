using System.Text.Json;
using DiagnosticsClient.Entity;
using DiagnosticsClient.Service;
using DiagnosticsClient.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiagnosticsClient.Workers;

public class DataFetchWorker : BackgroundService
{
    private readonly DataService _dataService;
    private readonly ServerService _serverService;
    private readonly HttpClient _http = new();

    public DataFetchWorker(
        DataService dataService, 
        ServerService serverService)
    {
        _dataService = dataService;
        _serverService = serverService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            List<ServerEntity> servers = await _serverService.LoadServersAsync();
            Console.WriteLine("Servers loaded:" + servers.Count());
            foreach (var server in servers)
            {
                if(!server.IsActive)
                  continue;
                try
                {
                    var req = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"{server.Url}/diagnostics");

                    req.Headers.Add("X-API-Key", server.ApiKey);

                    Console.WriteLine("sending request");
                    var res = await _http.SendAsync(req, stoppingToken);
                    Console.WriteLine("request complete");
                    res.EnsureSuccessStatusCode();
                    var json = await res.Content.ReadAsStringAsync(stoppingToken);

                    var metric = JsonSerializer.Deserialize<MetricEntity>(json, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    metric!.ServerId = server.Id;

                    await _dataService.AddMetricAsync(metric);
                }
                catch (Exception e)
                {
                    await _dataService.UpdateServerStateAsync(new ServerState()
                        {
                          ServerId = server.Id,
                          Ram = 0,
                          Disk = 0,
                          IsOk = false,
                          Error = $"Load metrics from server failed ({e.Message})"
                        });
                    Console.WriteLine("Load metrics failed: " + e.Message);
                }
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
