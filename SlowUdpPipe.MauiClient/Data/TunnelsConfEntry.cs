using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.ViewModels;

namespace SlowUdpPipe.MauiClient.Data;

public record TunnelsConfEntry(
    Guid Guid,
    string Name,
    string LocalAddress,
    string RemoteAddress,
    EncryptionAlgorithm EncryptionAlgo,
    string Key,
    bool Enabled)
{
  public static TunnelsConfEntry FromModel(TunnelEditViewModel _model)
  {
    return new TunnelsConfEntry(
      _model.Guid,
      _model.Name,
      _model.LocalAddress,
      _model.RemoteAddress,
      Consts.ENCRYPTION_ALG_SLUG_REVERSE[_model.EncryptionAlgo],
      _model.Key,
      _model.Enabled);
  }
};
