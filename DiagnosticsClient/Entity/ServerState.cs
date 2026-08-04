using System.ComponentModel.DataAnnotations.Schema;

namespace DiagnosticsClient.Entity;

public class ServerState
{
  public long? Id {get; set;}
  public long? ServerId {get; set;}
  public DateTime LastUpdate {get; set;} = DateTime.Now;
  public int Ram {get; set;}
  public int Disk {get; set;}
  public bool IsOk {get; set;} = false;
  public string? Error {get; set;}

  [ForeignKey(nameof(ServerId))]
  public ServerEntity? Server {get; set;}
}
