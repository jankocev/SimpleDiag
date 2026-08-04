// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using DiagnosticsClient.Commands;
using DiagnosticsClient.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.RegisterComponents();
var host = builder.Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MetricsContext>>().CreateDbContext();
db.Database.EnsureCreated();
db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

var root = new RootCommand();

var daemon = new Command("daemon");

daemon.SetAction(async (opt) =>
{
    await host.RunAsync();
});

root.Add(daemon);
root.Add(ServerCommand.Create(host.Services));
root.Add(ShowCommand.Create(host.Services));
await root.Parse(args).InvokeAsync();
