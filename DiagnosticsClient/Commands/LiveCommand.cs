using System.CommandLine;
using DiagnosticsClient.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace DiagnosticsClient.Commands;

public class LiveCommand
{
    public static Command Create(IServiceProvider services)
    {
        var cmd = new Command("live");
        cmd.SetAction(
            async (parseResult, cancelToken) =>
            {
                using var scope = services.CreateScope();
                var liveRunner = scope.ServiceProvider.GetRequiredService<LiveDataShowWorker>();
                await liveRunner.ExecuteAsync(cancelToken);
            }
        );

        return cmd;
    }
}
