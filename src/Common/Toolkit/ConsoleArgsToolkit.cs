using Ax.Fw.Extensions;

namespace CloakTunnel.Common.Toolkit;

public static class ConsoleArgsToolkit
{
  public static IList<string> AddEnvVarAsArg(
    this IList<string> _args, 
    string _envVarName, 
    string _argName)
  {
    var envValue = Environment.GetEnvironmentVariable(_envVarName);
    if (!envValue.IsNullOrWhiteSpace())
    {
      _args.Add(_argName);
      _args.Add(envValue);
    }
    return _args;
  }
}
