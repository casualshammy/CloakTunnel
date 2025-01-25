using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Server.Modules.WebServer.Controllers;
using CloakTunnel.Server.Modules.WebServer.Middlewares;
using CloakTunnel.Server.Modules.WebSocketController.Parts;
using System.Net;
using System.Reactive.Linq;
using ILog = Ax.Fw.SharedTypes.Interfaces.ILog;

namespace CloakTunnel.Server.Modules.WebServer;

internal class WebServerImpl
{
  private readonly UdpTunnelServerOptions p_options;
  private readonly ILog p_logger;
  private readonly WsServer p_wsServer;

  public WebServerImpl(
    UdpTunnelServerOptions _options,
    ILog _logger,
    IReadOnlyLifetime _lifetime)
  {
    p_options = _options;
    p_logger = _logger;
    p_wsServer = new WsServer(_lifetime);

    var thread = new Thread(async () =>
    {
      try
      {
        _logger.Info($"Starting server on {_options.LocalEndpoint}...");

        using (var host = CreateWebHost(_options.LocalEndpoint.Host, _options.LocalEndpoint.Port))
          await host.RunAsync(_lifetime.Token);

        _logger.Info($"Server on {_options.LocalEndpoint} is stopped");
      }
      catch (Exception ex)
      {
        _logger.Error($"Error in thread: {ex}");
      }
    });

    thread.IsBackground = true;
    thread.Start();

    WsServer = p_wsServer;
  }

  public WsServer WsServer { get; }

  private IHost CreateWebHost(
    string _host,
    int _port)
  {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.ClearProviders();

    builder.WebHost.ConfigureKestrel(_opt =>
    {
      _opt.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(130);
      _opt.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(90);
      _opt.Listen(IPAddress.Parse(_host), _port);
    });

    builder.Services.AddSingleton(p_logger);

    var controller = new ApiControllerV0(
      p_options,
      p_wsServer,
      p_logger);

    var app = builder.Build();
    app
      .UseMiddleware<LogMiddleware>()
      .UseWebSockets(new WebSocketOptions()
      {
        KeepAliveInterval = TimeSpan.FromSeconds(30)
      });

    app.MapGet(p_options.LocalEndpoint.AbsolutePath, controller.StartWebSocketAsync);

    return app;
  }

}

