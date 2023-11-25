using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Toolkit;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using static SlowUdpPipe.MauiClient.Data.AppConsts;

namespace SlowUdpPipe.MauiClient.ViewModels;

internal class TunnelsListViewModel : ObservableModel
{
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private readonly ObservableCollection<TunnelEditViewModel> p_defs;

  public TunnelsListViewModel()
  {
    var lifetime = Container.Locate<IReadOnlyLifetime>();
    p_tunnelsConfCtrl = Container.Locate<ITunnelsConfCtrl>();

    p_defs = new ObservableCollection<TunnelEditViewModel>();
    foreach (var tunnelEntry in p_tunnelsConfCtrl.GetConfs().OrderBy(_ => _.Name))
    {
      var tunnelModel = new TunnelEditViewModel(tunnelEntry);
      p_defs.Add(tunnelModel);
    }

    OnPropertyChanged(nameof(Data));

    p_tunnelsConfCtrl.TunnelConfRemoved
      .Subscribe(_guid =>
      {
        var existingValue = p_defs.FirstOrDefault(_ => _.Guid == _guid);
        if (existingValue != null)
          p_defs.Remove(existingValue);
      }, lifetime);

    p_tunnelsConfCtrl.TunnelConfAdded
      .Subscribe(_conf =>
      {
        p_defs.Add(new TunnelEditViewModel(_conf));
      }, lifetime);

    p_tunnelsConfCtrl.TunnelConfChanged
      .Subscribe(_conf =>
      {
        var existingValue = p_defs.FirstOrDefault(_ => _.Guid == _conf.Guid);
        if (existingValue == null)
          return;

        existingValue.Name = _conf.Name;
        existingValue.LocalAddress = _conf.LocalAddress;
        existingValue.RemoteAddress = _conf.RemoteAddress;
        existingValue.EncryptionAlgo = Common.Data.Consts.ENCRYPTION_ALG_SLUG[_conf.EncryptionAlgo];
        existingValue.CipheredKey = _conf.Key;
        existingValue.Enabled = _conf.Enabled;
      }, lifetime);
  }

  public string Title { get; } = "SlowUdpPipe";

  public ObservableCollection<TunnelEditViewModel> Data => p_defs;

  public async Task<TunnelEditViewModel?> TryGetExistingTunnelModelAsync(Guid _tunnelGuid, CancellationToken _ct)
  {
    var counter = 0;
    while (!_ct.IsCancellationRequested && counter < 50)
    {
      var model = p_defs.FirstOrDefault(_ => _.Guid == _tunnelGuid);
      if (model != null)
        return model;

      await Task.Delay(100, _ct);
      ++counter;
    }

    return null;
  }

}
