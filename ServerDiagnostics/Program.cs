using System.Diagnostics;
using System.Text.Json;

public class Program
{
    private const string ApiKeyHeader = "X-Api-Key";

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile(
            "/etc/ServerDiagnostics/config.json",
            optional: true,
            reloadOnChange: true
        );

        string apiKey = builder.Configuration["ApiKey"] ?? "";
        var app = builder.Build();

        app.MapGet(
            "/diagnostics",
            async (HttpContext ctx) =>
            {
                if (
                    !ctx.Request.Headers.TryGetValue(ApiKeyHeader, out var clientApiKey)
                    || clientApiKey != apiKey
                )
                {
                    return Results.Unauthorized();
                }
                var ramInfo = GetRamInfo();
                var freeDisk = GetFreeDiskSpace("/");
                var dockerCount = await GetDockerServiceCount();
                var metric = new MetricEntity()
                {
                    TotalRam = ramInfo.total,
                    UsedRam = ramInfo.used,
                    DiskSize = freeDisk.TotalSize,
                    UsedDiskSize = freeDisk.TotalSize - freeDisk.AvailableFreeSpace,
                    DockerServiceCount = dockerCount,
                };

                Console.WriteLine(JsonSerializer.Serialize(metric));
                return Results.Ok(metric);
            }
        );

        app.Run();

        static (long total, long used) GetRamInfo()
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            Console.Write(lines);
            Console.WriteLine("");

            long total = ParseKb(lines, "MemTotal");
            long free = ParseKb(lines, "MemAvailable");

            return (total * 1024, (total - free) * 1024);
        }

        static long ParseKb(string[] lines, string key)
        {
            var line = lines.First(l => l.StartsWith(key));
            var parts = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return long.Parse(parts[1]);
        }

        static DriveInfo GetFreeDiskSpace(string path)
        {
            return new DriveInfo(path);
        }

        static async Task<int> GetDockerServiceCount()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps -q",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using var process = Process.Start(psi);

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}

public class MetricEntity
{
    public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    public long TotalRam { get; set; } = 0;
    public long UsedRam { get; set; } = 0;
    public long DiskSize { get; set; } = 0;
    public long UsedDiskSize { get; set; } = 0;
    public int? DockerServiceCount { get; set; } = 0;
}
