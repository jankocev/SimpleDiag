using System.CommandLine;
using DiagnosticsClient.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace DiagnosticsClient.Commands;

public static class ServerCommand
{
    public static Command Create(IServiceProvider services)
    {
        var server = new Command("server");
        server.SetAction(async opt => 
            {
                using var scope = services.CreateScope();
                var serverMenu = scope.ServiceProvider.GetRequiredService<ServerMenu>();
                await serverMenu.Show();

            });

        return server;
    }
}
