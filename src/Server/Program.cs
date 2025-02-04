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
    using var logger = new GenericLog()
      .AttachConsoleLog();

    if (_args?.Length == 1 && _args[0] == "test")
    {
      EncryptionAlgorithmsTest.TestAndPrintInConsole(lifetime, logger);
      await Task.Delay(TimeSpan.FromSeconds(1));
      return;
    }

    if (_args?.Length == 1 && _args[0] == "genkey")
    {
      logger.Info(Utilities.GetRandomString(32, false));
      await Task.Delay(TimeSpan.FromSeconds(1));
      return;
    }

    var args = new List<string>((_args ?? []))
      .AddEnvVarAsArg("CLOAK_TUNNEL_BIND", "-b")
      .AddEnvVarAsArg("CLOAK_TUNNEL_FORWARD", "-f")
      .AddEnvVarAsArg("CLOAK_TUNNEL_PASSKEY", "-p")
      .AddEnvVarAsArg("CLOAK_TUNNEL_CIPHER", "-c")
      .ToArray();

    var options = (TunnelServerOptions?)null;
    var argParseSuccess = FluentArgsBuilder
      .New()
      .RegisterDefaultHelpFlags()
      .Parameter<string>("-b", "--bind")
        .WithDescription("Bind input traffic to this address")
        .WithValidation(_arg =>
        {
          if (!Uri.TryCreate(_arg, UriKind.Absolute, out var uri))
            return false;

          if (uri.Scheme == "ws" && uri.AbsolutePath.Length < 4)
            return false;

          return uri.Scheme == "udp" || uri.Scheme == "ws";
        }, "Argument must be in format 'udp://<ip-address>:<port>' or 'ws://<ip-address>:<port>/<ws-path>'; <ws-path> must be at least 4 characters long")
        .WithExamples("udp://0.0.0.0:1935", "ws://0.0.0.0:8088/ws-path")
        .IsRequired()
      .Parameter<string>("-f", "--forward")
        .WithDescription("Forward traffic to this endpoint")
        .WithValidation(_arg =>
        {
          if (!Uri.TryCreate(_arg, UriKind.Absolute, out var uri))
            return false;

          return uri.Scheme == "udp";
        }, "Argument must be in format 'udp://<ip-address>:<port>'")
        .WithExamples("udp://127.0.0.1:51820")
        .IsRequired()
      .Parameter<string>("-p", "--passkey")
        .WithDescription("Key to encrypt traffic")
        .WithValidation(_arg => _arg.Length >= 8, "Argument must be a string at least 8 characters long")
        .WithExamples(Utilities.GetRandomString(8, false))
        .IsRequired()
      .Parameter<string>("-c", "--cipher")
        .WithDescription($"Cipher algorithm to use in encryption (default: {EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256]})")
        .WithValidation(
          EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE.ContainsKey, 
          $"Encryption algorithm must be one of: {string.Join(", ", EncryptionToolkit.ENCRYPTION_ALG_SLUG.Values)}")
        .WithExamples(EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256], EncryptionToolkit.ENCRYPTION_ALG_SLUG.Values.ToArray())
        .IsOptionalWithDefault(EncryptionToolkit.ENCRYPTION_ALG_SLUG[EncryptionAlgorithm.AesGcmObfs256])
      .Call(_cipher => _passKey => _forwardUri => _bindUri =>
      {
        if (Uri.TryCreate(_bindUri, UriKind.Absolute, out var bindUri) 
          && Uri.TryCreate(_forwardUri, UriKind.Absolute, out var forwardUri)
          && EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE.TryGetValue(_cipher, out var encAlgo))
          options = new TunnelServerOptions(
            bindUri.Scheme.StartsWith("udp") ? EndpointType.Udp : EndpointType.Websocket,
            bindUri,
            forwardUri,
            encAlgo,
            _passKey);
      })
      .Parse(args);

    if (!argParseSuccess || options == null)
    {
      logger.Error($"Argument parsing error");
      await Task.Delay(TimeSpan.FromSeconds(1));
      return;
    }

    var settingsProvider = new SettingsProviderImpl(options);

    lifetime.DoOnEnded(() =>
    {
      logger.Info($"-------------------------------------------");
      logger.Info($"CloakTunnel Server stopped");
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

}
