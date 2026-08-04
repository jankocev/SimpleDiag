using System.CommandLine;
using System.Text.Json;
using DiagnosticsClient.Service;
using Microsoft.Extensions.DependencyInjection;

namespace DiagnosticsClient.Commands;

public class ShowCommand
{

    public static Command Create(IServiceProvider services)
    {
        var server = new Command("show");

        server.SetAction(async (parseResult) =>
        {
            using var scope = services.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
            var res = await dataService.LoadServerStatesAsync();

            Console.WriteLine(JsonSerializer.Serialize(res.Select(m => new OutputModel()
            {
                Name = m.Server!.Name,
                Url = m.Server!.Url,
                Time = m.LastUpdate,
                Ram = m.Ram,
                Disk = m.Disk,
                IsOk = m.IsOk,
                Error = m.Error
            }).ToList()));
        });
        return server;
    }

    public class OutputModel
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Error {get; set;}
        public DateTime? Time { get; set; }
        public int Ram { get; set; } = 0;
        public int Disk { get; set; } = 0;
        public bool IsOk {get; set;} = false;
    }

}

public class Helper
{
    public static int GetPercent(long parcial, long full)
    {
        if (full == 0)
            return 0;
        var res = (int)((parcial * 100 / full));
        return res;
    }
}
