using DiagnosticsClient.Menu;
using DiagnosticsClient.Service;
using DiagnosticsClient.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiagnosticsClient.Utils;

public static class DIExtension
{
    public static HostApplicationBuilder RegisterComponents(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders(); // remove default console logger
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Warning; // only warnings/errors
        });
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.RegisterDatabase();

        builder.Services.AddHostedService<DataFetchWorker>();

        builder.Services.AddScoped<ServerMenu>();
        builder.Services.AddTransient<ServerService>();
        builder.Services.AddTransient<DataService>();
        builder.Services.AddTransient<LiveDataShowWorker>();

        return builder;
    }

    public static HostApplicationBuilder RegisterDatabase(this HostApplicationBuilder builder)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appData = Path.Combine(home, "diagnostics-client");
        Directory.CreateDirectory(appData);

        var dbPath = Path.Combine(appData, "metrics.db");
        builder.Services.AddDbContextFactory<MetricsContext>(o =>
        {
            o.UseSqlite($"Data Source={dbPath}");
            o.LogTo(_ => { }, Microsoft.Extensions.Logging.LogLevel.None);
        });
        return builder;
    }
}
