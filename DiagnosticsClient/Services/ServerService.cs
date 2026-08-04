using DiagnosticsClient.Entity;
using DiagnosticsClient.Utils;
using Microsoft.EntityFrameworkCore;

namespace DiagnosticsClient.Service;

public class ServerService
{
  private readonly IDbContextFactory<MetricsContext> _ctxFct;

    public ServerService(IDbContextFactory<MetricsContext> ctxFct)
    {
        _ctxFct = ctxFct;
    }

    internal async Task CreateServerAsync(ServerEntity serverEntity)
    {
      await using var ctx = await _ctxFct.CreateDbContextAsync();
      var r = await ctx.Servers.AddAsync(serverEntity);
      await ctx.SaveChangesAsync();
    }

    internal async Task DeleteAsync(ServerEntity selected)
    {
      await using var ctx = await _ctxFct.CreateDbContextAsync();
      var target = await ctx.Servers.Include(s => s.Metrics)
        .Include(s => s.State).FirstOrDefaultAsync(s => s.Id == selected.Id);
      
      ctx.Servers.Remove(target!);
      await ctx.SaveChangesAsync();

    }

    internal async Task<List<ServerEntity>> LoadServersAsync()
    {
       await using var ctx = await _ctxFct.CreateDbContextAsync();
       return await ctx.Servers.ToListAsync();
    }
}
