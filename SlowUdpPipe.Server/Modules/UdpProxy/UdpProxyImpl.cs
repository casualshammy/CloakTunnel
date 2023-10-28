using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;
using SlowUdpPipe.Interfaces;
using SlowUdpPipe.Server.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SlowUdpPipe.Modules.UdpProxy;

[ExportClass(typeof(UdpProxyImpl), Singleton: true, ActivateOnStart: true)]
internal class UdpProxyImpl
{
  private readonly ILogger p_logger;
  private int p_serverCounter = -1;

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
          if (!CheckDef(defSlug, def, out var remote, out var local, out var key, out var algorithms))
            continue;

          var opt = new UdpTunnelServerOptions(remote, local, algorithms, key);
          var serverIndex = Interlocked.Increment(ref p_serverCounter);
          var logger = _logger[$"{serverIndex}-{defSlug}"];
          var algosEE = opt.Algorithms
            .Select(_ => Consts.ENCRYPTION_ALG_SLUG[_])
            .OrderBy(_ => _);

          logger.Warn($"Launching udp tunnel L:{opt.Local} > R:{opt.Remote}; algorithms: ({string.Join(", ", algosEE)})...");
          _ = new UdpTunnelServer(opt, _life, logger);
        }
      });
  }

  private bool CheckDef(
    string? _defSlug,
    UdpTunnelServerRawOptions _options,
    [NotNullWhen(true)] out IPEndPoint? _remote,
    [NotNullWhen(true)] out IPEndPoint? _local,
    [NotNullWhen(true)] out string? _key,
    [NotNullWhen(true)] out IReadOnlyList<EncryptionAlgorithm>? _algorithms)
  {
    _remote = null;
    _local = null;
    _key = null;
    _algorithms = null;

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

    var ciphers = new HashSet<EncryptionAlgorithm>();
    if (_options.Ciphers == null)
    {
      ciphers = new(Consts.ALL_CYPHERS);
    }
    else
    {
      foreach (var cipherSlug in _options.Ciphers)
      {
        if (cipherSlug == null || !Consts.ENCRYPTION_ALG_SLUG_REVERSE.TryGetValue(cipherSlug, out var algo))
        {
          p_logger.Warn($"Definition '{_defSlug}' contains unknown encryption algorithm '{cipherSlug}'! Please refer to documentation");
          continue;
        }

        ciphers.Add(algo);
      }
    }

    if (!ciphers.Any())
    {
      p_logger.Error($"Definition '{_defSlug}' doesn't contain any encription algorithm! Please refer to documentation");
      return false;
    }

    _remote = remote;
    _local = local;
    _key = _options.Key;
    _algorithms = ciphers.ToArray();
    return true;
  }

}
