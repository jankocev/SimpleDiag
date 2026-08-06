using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiagnosticsClient.Entity;

public class MetricEntity
{
    public long? Id { get; set; }

    [Required]
    public long? ServerId { get; set; }
    public DateTime? Timestamp { get; set; }
    public long TotalRam { get; set; } = 0;
    public long UsedRam { get; set; } = 0;
    public long DiskSize { get; set; } = 0;
    public long UsedDiskSize { get; set; } = 0;
    public int DockerServiceCount { get; set; } = 0;

    [ForeignKey(nameof(ServerId))]
    public ServerEntity? Server { get; set; }
}
