using System.Diagnostics.CodeAnalysis;

namespace CloakTunnel.Common.Toolkit;

public static class UriToolkit
{
  public static bool CheckUdpOrWsOrWssUri(
    string? _rawUri, 
    [NotNullWhen(true)] out Uri? _uri)
  {
    _uri = null;

    if (!Uri.TryCreate(_rawUri, UriKind.Absolute, out var uri))
      return false;

    if ((uri.Scheme == "ws" || uri.Scheme == "wss") && uri.AbsolutePath.Length < 4)
      return false;

    _uri = uri;
    return uri.Scheme == "udp" || uri.Scheme == "ws" || uri.Scheme == "wss";
  }

  public static bool CheckUdpOrWsUri(
    string? _rawUri,
    [NotNullWhen(true)] out Uri? _uri)
  {
    _uri = null;

    if (!Uri.TryCreate(_rawUri, UriKind.Absolute, out var uri))
      return false;

    if (uri.Scheme == "ws" && uri.AbsolutePath.Length < 4)
      return false;

    _uri = uri;
    return uri.Scheme == "udp" || uri.Scheme == "ws";
  }

  public static bool CheckUdpUri(
    string? _rawUri,
    [NotNullWhen(true)] out Uri? _uri)
  {
    _uri = null;

    if (!Uri.TryCreate(_rawUri, UriKind.Absolute, out var uri))
      return false;

    _uri = uri;
    return uri.Scheme == "udp";
  }

}
