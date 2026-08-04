using System.Text.Json.Serialization;

namespace DiagnosticsClient.Entity;

public class ServerEntity
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public ServerState State {get; set;} = new();
    [JsonIgnore]
    public List<MetricEntity> Metrics { get; set; } = new();
}
