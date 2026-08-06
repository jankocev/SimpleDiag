using System.Text.Json;
using DiagnosticsClient.Entity;
using DiagnosticsClient.Service;
using DiagnosticsClient.Utils;
using Spectre.Console;

namespace DiagnosticsClient.Menu;

public class ServerMenu : IMenu
{
    private readonly ServerService _serverService;
    private readonly DataService _dataService;

    public ServerMenu(DataService dataService, ServerService serverService)
    {
        _dataService = dataService;
        _serverService = serverService;
    }

    public async Task Show()
    {
        var exit = false;

        while (!exit)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Server configuration")
                    .AddChoices(
                        new[] { "List servers", "History", "Add server", "Remove server", "Exit" }
                    )
            );

            switch (choice)
            {
                case "List servers":
                    await ListServers();
                    break;

                case "Add server":
                    await AddServer();
                    break;

                case "History":
                    await ShowHistory();
                    break;

                case "Remove server":
                    await RemoveServer();
                    break;

                case "Exit":
                    exit = true;
                    break;
            }
        }
    }

    async Task ShowHistory()
    {
        var servers = await _serverService.LoadServersAsync();
        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers configured[/]");
            return;
        }
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<ServerEntity>()
                .Title("Select server:")
                .UseConverter(x => $"{x.Name} ({x.Url})")
                .AddChoices(servers)
        );
        var details = await _dataService.LoadAsync(selected.Id!.Value);
        Console.Write(JsonSerializer.Serialize(details));
    }

    async Task ListServers()
    {
        var servers = await _serverService.LoadServersAsync();
        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("URL");
        table.AddColumn("Key");

        foreach (var s in servers)
        {
            table.AddRow(
                s.Id.ToString(),
                s.Name ?? "undefined",
                s.Url ?? "undefined",
                s.ApiKey ?? "undefined"
            );
        }

        AnsiConsole.Write(table);
    }

    async Task AddServer()
    {
        var name = AnsiConsole.Ask<string>("Server [green]name[/]:");
        var desc = AnsiConsole.Ask<string>("Description:");
        var url = AnsiConsole.Ask<string>("Server [green]URL[/]:");
        var key = AnsiConsole.Prompt(new TextPrompt<string>("API key:").Secret());

        await _serverService.CreateServerAsync(
            new ServerEntity
            {
                Name = name,
                Url = url,
                ApiKey = key,
                Description = desc,
                State = new()
                {
                    IsOk = false,
                    Error = "Server is freshly created, no data are recoreded yet",
                },
            }
        );

        AnsiConsole.MarkupLine($"[green]Server {name} added.[/]");
    }

    async Task RemoveServer()
    {
        var servers = await _serverService.LoadServersAsync();
        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers configured[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<ServerEntity>()
                .Title("Select server to remove")
                .UseConverter(x => $"{x.Name} ({x.Url})")
                .AddChoices(servers)
        );

        await _serverService.DeleteAsync(selected);
        AnsiConsole.MarkupLine($"[red]Server {selected.Name} removed[/]");
    }
}
