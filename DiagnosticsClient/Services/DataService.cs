using System.Diagnostics;
using System.Text.Json;
using DiagnosticsClient.Entity;
using DiagnosticsClient.Utils;
using Microsoft.EntityFrameworkCore;

namespace DiagnosticsClient.Service;

public class DataService
{
    private readonly IDbContextFactory<MetricsContext> _ctxFct;
    private readonly ServerService _serverService;
    private readonly HttpClient _http = new();

    public DataService(IDbContextFactory<MetricsContext> ctxFct, ServerService serverService)
    {
        _ctxFct = ctxFct;
        _serverService = serverService;
    }

    public async Task FetchData(ServerEntity server, CancellationToken stoppingToken)
    {
        List<ServerEntity> servers = await _serverService.LoadServersAsync();
        Debug.WriteLine("Servers loaded:" + servers.Count());

        if (!server.IsActive)
            return;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{server.Url}/diagnostics");

            req.Headers.Add("X-Api-Key", server.ApiKey);

            Debug.WriteLine("sending request");
            var res = await _http.SendAsync(req, stoppingToken);
            Debug.WriteLine("request complete");
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync(stoppingToken);

            var metric = JsonSerializer.Deserialize<MetricEntity>(
                json,
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
            );
            metric!.ServerId = server.Id;

            await AddMetricAsync(metric);
        }
        catch (Exception e)
        {
            await UpdateServerStateAsync(
                new ServerState()
                {
                    ServerId = server.Id,
                    Ram = 0,
                    Disk = 0,
                    IsOk = false,
                    Error = $"Load metrics from server failed ({e.Message})",
                }
            );
            Debug.Fail("Load metrics failed: " + e.Message);
        }
    }

    public async Task<List<MetricEntity>> LoadAsync(long serverId)
    {
        await using var ctx = await _ctxFct.CreateDbContextAsync();
        return await ctx.Metrics.Where(m => m.ServerId == serverId).ToListAsync();
    }

    internal async Task LoadServerMetricsAsync()
    {
        await using var ctx = await _ctxFct.CreateDbContextAsync();

        var data = await ctx
            .Metrics.Include(m => m.Server)
            .GroupBy(m => m.ServerId)
            .Select(g => g.OrderByDescending(m => m.Timestamp).First())
            .ToListAsync();
    }

    internal async Task<List<ServerState>> LoadServerStatesAsync()
    {
        await using var ctx = await _ctxFct.CreateDbContextAsync();
        return await ctx.State.Include(s => s.Server).Where(s => s.Server!.IsActive).ToListAsync();
    }

    public async Task UpdateServerStateAsync(ServerState state)
    {
        await using var ctx = await _ctxFct.CreateDbContextAsync();
        var target = await ctx.State.FirstOrDefaultAsync(s => s.ServerId == state.ServerId);

        if (target == null)
        {
            target = state;
        }
        else
        {
            target.Error = state.Error;
            target.IsOk = state.IsOk;
            target.Disk = state.Disk;
            target.Ram = state.Ram;
            target.LastUpdate = DateTime.Now;
            target.DockerServiceCount = state.DockerServiceCount;
        }
        ctx.State.Update(target);
        await ctx.SaveChangesAsync();
    }

    public async Task CleanOldRecordsAsync()
    {
        await using (var db = await _ctxFct.CreateDbContextAsync())
        {
            var date = DateTime.UtcNow.AddDays(-5);
            db.Metrics.RemoveRange(db.Metrics.Where(m => m.Timestamp < date));
            await db.SaveChangesAsync();
        }
    }

    public async Task AddMetricAsync(MetricEntity metric)
    {
        ServerState state = new() { ServerId = metric.ServerId, IsOk = true };
        await using (var ctx = await _ctxFct.CreateDbContextAsync())
        {
            await ctx.Metrics.AddAsync(metric);
            await ctx.SaveChangesAsync();
        }
        state.Ram = GetPersentage(metric.UsedRam, metric.TotalRam);
        state.Disk = GetPersentage(metric.UsedDiskSize, metric.DiskSize);
        state.DockerServiceCount = metric.DockerServiceCount;
        if (state.Ram == 0)
        {
            state.IsOk = false;
            state.Error = "Read ram usage failed";
        }
        else if (state.Ram > 70)
        {
            state.IsOk = false;
            state.Error = "More then 70 percent of ram is used";
        }
        else if (state.Disk == 0)
        {
            state.IsOk = false;
            state.Error = "Read disk usage failed";
        }
        else if (state.Disk > 70)
        {
            state.IsOk = false;
            state.Error = "Disk is more than 70% full";
        }

        await UpdateServerStateAsync(state);
    }

    private int GetPersentage(long part, long total)
    {
        if (total == 0)
            return 0;
        return (int)((part * 100) / total);
    }
}
