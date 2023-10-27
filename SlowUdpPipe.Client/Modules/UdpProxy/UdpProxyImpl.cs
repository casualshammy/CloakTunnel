using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Client.Data;
using SlowUdpPipe.Client.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SlowUdpPipe.Client.Modules.UdpProxy;

[ExportClass(typeof(UdpProxyImpl), Singleton: true, ActivateOnStart: true)]
internal class UdpProxyImpl
{
  private readonly ILogger p_logger;
  private int p_clientCounter = -1;

  public UdpProxyImpl(
    ISettingsProvider _settingsProvider,
    IReadOnlyLifetime _lifetime,
    ILogger _logger)
  {
    p_logger = _logger;

    _settingsProvider.Definitions
      .HotAlive(_lifetime, (_defs, _life) =>
      {
        if (_defs == null || _defs.Count == 0)
        {
          _logger.Warn($"Definitions list is empty! No tunnel will be started");
          return;
        }

        foreach (var (defSlug, def) in _defs)
        {
          if (!CheckDef(defSlug, def, out var remote, out var local, out var key, out var algorithm))
            continue;

          var opt = new UdpTunnelClientOptions(remote, local, algorithm.Value, key);
          var clientIndex = Interlocked.Increment(ref p_clientCounter);
          var logger = _logger[$"{clientIndex}-{defSlug}"];
          var algoSlug = Consts.ENCRYPTION_ALG_SLUG[opt.Cipher];

          logger.Warn($"Launching udp tunnel L:{opt.Local} > R:{opt.Remote}; algorithm: {algoSlug}...");
          _ = new UdpTunnelClient(opt, _life, logger);
        }
      });
  }

  private bool CheckDef(
    string? _defSlug,
    UdpTunnelClientRawOptions _options,
    [NotNullWhen(true)] out IPEndPoint? _remote,
    [NotNullWhen(true)] out IPEndPoint? _local,
    [NotNullWhen(true)] out string? _key,
    [NotNullWhen(true)] out EncryptionAlgorithm? _algorithm)
  {
    _remote = null;
    _local = null;
    _key = null;
    _algorithm = null;

    if (_defSlug.IsNullOrWhiteSpace())
    {
      p_logger.Error($"Definition's name must contain non-space characters!");
      return false;
    }

    if (!IPEndPoint.TryParse(_options.Remote, out var remote))
    {
      p_logger.Error($"Definition '{_defSlug}' contains wrong '{nameof(_options.Remote)}'! It must be in 'ip:port' format");
      return false;
    }
    if (!IPEndPoint.TryParse(_options.Local, out var local))
    {
      p_logger.Error($"Definition '{_defSlug}' contains wrong '{nameof(_options.Local)}'! It must be in 'ip:port' format");
      return false;
    }
    if (_options.Key.IsNullOrWhiteSpace())
    {
      p_logger.Error($"Definition '{_defSlug}' contains wrong '{nameof(_options.Key)}'! It must be non-whitespace string");
      return false;
    }

    if (_options.Cipher == null)
      _algorithm = Consts.DEFAULT_ENCRYPTION;
    else if (Consts.ENCRYPTION_ALG_SLUG_REVERSE.TryGetValue(_options.Cipher, out var algo))
      _algorithm = algo;
    else
    {
      p_logger.Error($"Definition '{_defSlug}' has unknown encription algorithm '{_options.Cipher}'! Please refer to documentation");
      return false;
    }


    _remote = remote;
    _local = local;
    _key = _options.Key;
    return true;
  }

}
