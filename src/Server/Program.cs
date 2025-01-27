using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.Extensions;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using CloakTunnel.Server.Interfaces;
using CloakTunnel.Server.Modules.SettingsProvider;
using CloakTunnel.Server.Modules.UdpProxy;
using FluentArgs;
using System.Reactive.Linq;
using System.Reflection;

namespace CloakTunnel.Server;

public class Program
{
  public static async Task Main(string[]? _args)
  {
    var assembly = Assembly.GetExecutingAssembly() ?? throw new Exception("Can't get assembly!");

    var lifetime = new Lifetime();
    using var logger = new GenericLog(null)
      .AttachConsoleLog();

    if (_args?.Length == 1 && _args[0] == "test")
    {
      EncryptionAlgorithmsTest.TestAndPrintInConsole(lifetime, logger);
      return;
    }

    if (_args?.Length == 1 && _args[0] == "genkey")
    {
      logger.Warn($"===== Use this key in tunnel's description =====");
      logger.Info(Utilities.GetRandomString(32, false));
      logger.Warn($"================================================");
      return;
    }

    var localAddress = (string?)null;
    var remoteAddress = (string?)null;
    var passKey = (string?)null;
    var cipher = (string?)null;
    FluentArgsBuilder
      .New()
      .Parameter<string>("-L", "--local")
        .WithDescription("Bind server to this address")
        .WithValidation(_ => _.StartsWith("udp://") || _.StartsWith("ws://"))
        .WithExamples("udp://0.0.0.0:4123", "ws://0.0.0.0:8088/endpoint")
        .IsRequired()
      .Parameter<string>("-R", "--remote")
        .WithDescription("Forward traffic to this endpoint")
        .WithValidation(_ => _.StartsWith("udp://"))
        .WithExamples("udp://127.0.0.1:51820")
        .IsRequired()
      .Parameter<string>("-p", "--passkey")
        .WithDescription("Key to encrypt traffic")
        .WithValidation(_ => _.Length >= 4)
        .WithExamples(Utilities.GetRandomString(8, false))
        .IsRequired()
      .Parameter<string>("-c", "--cipher")
        .WithDescription($"Cipher algorithm to use in encryption (default: {EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256]})")
        .WithValidation(EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE.ContainsKey)
        .WithExamples(EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256], EncryptionToolkit.ENCRYPTION_ALG_SLUG.Values.ToArray())
        .IsOptionalWithDefault(EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256])
      .Call(_cipher => _passKey => _remote => _local =>
      {
        localAddress = _local;
        remoteAddress = _remote;
        passKey = _passKey;
        cipher = _cipher;
      })
      .Parse(_args ?? []);

    var options = CreateOptions(logger, cipher, passKey, remoteAddress, localAddress);
    if (options == null)
      return;

    var settingsProvider = new SettingsProviderImpl(options);

    lifetime.DoOnEnded(() =>
    {
      logger.Info($"-------------------------------------------");
      logger.Info($"Server stopped");
      logger.Info($"-------------------------------------------");
    });

    var version = new SerializableVersion(assembly.GetName().Version ?? new Version(0, 0, 0, 0));
    logger.Info($"-------------------------------------------");
    logger.Info($"CloakTunnel Server Started");
    logger.Info($"Version: {version}");
    logger.Info($"OS: {Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}");
    logger.Info($"-------------------------------------------");

    _ = AppDependencyManager
      .Create()
      .AddSingleton<ILog>(logger)
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ISettingsProvider>(settingsProvider)
      .AddModule<TunnelControllerImpl>()
      .ActivateOnStart<TunnelControllerImpl>()
      .Build();

    lifetime.InstallConsoleCtrlCHook();

    try
    {
      //await Task.Delay(-1, lifetime.Token);
      await lifetime.OnEnd.FirstOrDefaultAsync();

      // wait for the logger to flush
      await Task.Delay(TimeSpan.FromSeconds(1));
    }
    catch (TaskCanceledException)
    {
      // ignore
    }
  }

  private static UdpTunnelServerOptions? CreateOptions(
    ILog _logger,
    string? _cipher,
    string? _passKey,
    string? _remote,
    string? _local)
  {
    if (_cipher.IsNullOrWhiteSpace() || !EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE.TryGetValue(_cipher, out var encAlgo))
    {
      _logger.Warn($"Unknown encryption algorithm '{_cipher}'! Please refer to documentation");
      return null;
    }

    if (_passKey.IsNullOrWhiteSpace() || _passKey.Length < 4)
    {
      _logger.Warn($"Passkey is too short (min: 4 characters)");
      return null;
    }

    if (_remote.IsNullOrWhiteSpace())
    {
      _logger.Warn($"Remote endpoint is empty");
      return null;
    }

    var remoteUri = new Uri(_remote);

    if (_local.IsNullOrWhiteSpace())
    {
      _logger.Warn($"Local endpoint is empty");
      return null;
    }

    var localUri = new Uri(_local);

    if (localUri.Scheme.StartsWith("wss"))
    {
      _logger.Warn($"Secure websocket is not supported; use reverse proxy like nginx");
      return null;
    }

    if (localUri.Scheme.StartsWith("ws") && localUri.AbsolutePath.Length < 3)
    {
      _logger.Warn($"Websocket path (section after last '/') must be at least 2 characters long");
      return null;
    }

    return new UdpTunnelServerOptions(
      localUri.Scheme.StartsWith("udp") ? EndpointType.Udp : EndpointType.Websocket,
      localUri,
      remoteUri,
      encAlgo,
      _passKey);
  }

}
