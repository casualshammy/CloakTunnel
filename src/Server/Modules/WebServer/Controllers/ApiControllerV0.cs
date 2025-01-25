using CloakTunnel.Common.Data;
using CloakTunnel.Server.Modules.WebSocketController.Parts;
using Microsoft.AspNetCore.Mvc;
using ILog = Ax.Fw.SharedTypes.Interfaces.ILog;

namespace CloakTunnel.Server.Modules.WebServer.Controllers;

internal class ApiControllerV0
{
  private static long p_wsSessionsCount = 0;
  private static long p_reqCount = -1;

  private readonly UdpTunnelServerOptions p_options;
  private readonly WsServer p_wsServer;
  private readonly ILog p_log;

  public ApiControllerV0(
    UdpTunnelServerOptions _options,
    WsServer _wsServer,
    ILog _logger)
  {
    p_options = _options;
    p_wsServer = _wsServer;
    p_log = _logger;
  }

  //[HttpGet("/ws")]
  public async Task<IResult> StartWebSocketAsync(
    HttpRequest _httpRequest,
    [FromQuery(Name = "key")] string? _key,
    CancellationToken _ct)
  {
    if (p_options.PassKey != _key)
      return Results.BadRequest();

    if (!_httpRequest.HttpContext.WebSockets.IsWebSocketRequest)
      return Results.BadRequest();

    var log = GetLog(_httpRequest);

    var sessionIndex = Interlocked.Increment(ref p_wsSessionsCount);
    var guid = Guid.NewGuid();
    log.Info($"Establishing WS connection '{sessionIndex}', id: {guid}...");

    using var websocket = await _httpRequest.HttpContext.WebSockets.AcceptWebSocketAsync();
    _ = await p_wsServer.AcceptSocketAsync(guid, websocket);
    log.Info($"WS connection '{sessionIndex}' (id: {guid}) is closed");

    return Results.Empty;
  }

  private ILog GetLog(HttpRequest _httpRequest)
  {
    var ip = _httpRequest.HttpContext.Connection.RemoteIpAddress;
    return p_log[Interlocked.Increment(ref p_reqCount).ToString()][$"{ip}"];
  }

}
