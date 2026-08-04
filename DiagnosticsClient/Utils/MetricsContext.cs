using Microsoft.EntityFrameworkCore;
using DiagnosticsClient.Entity;

namespace DiagnosticsClient.Utils;

public class MetricsContext : DbContext
{
    public MetricsContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ServerEntity> Servers { get; set; }
    public DbSet<MetricEntity> Metrics { get; set; }
    public DbSet<ServerState> State {get; set;}
}
