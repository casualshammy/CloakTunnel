using Ax.Fw.SharedTypes.Data;
using Ax.Fw;
using CloakTunnel.Common.Data;
using CloakTunnel.Server.Modules.WebSocketController.Parts;
using Microsoft.AspNetCore.Mvc;
using ILog = Ax.Fw.SharedTypes.Interfaces.ILog;

namespace CloakTunnel.Server.Modules.WebServer.Controllers;

internal class ApiControllerV0
{
  private static long p_wsSessionsCount = -1;
  private static long p_reqCount = -1;

  private readonly string p_passKeyHash;
  private readonly WsServer p_wsServer;
  private readonly ILog p_log;

  public ApiControllerV0(
    TunnelServerOptions _options,
    WsServer _wsServer,
    ILog _logger)
  {
    p_passKeyHash = Cryptography.CalculateSHAHash(_options.PassKey, HashComplexity.Bit512);
    p_wsServer = _wsServer;
    p_log = _logger;
  }

  //[HttpGet("/ws")]
  public async Task<IResult> StartWebSocketAsync(
    HttpRequest _httpRequest,
    [FromQuery(Name = "key")] string? _keyHash,
    CancellationToken _ct)
  {
    if (p_passKeyHash != _keyHash)
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
