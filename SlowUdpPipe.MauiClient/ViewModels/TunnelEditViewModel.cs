using Ax.Fw.Extensions;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Modules.TunnelsConfCtrl;
using SlowUdpPipe.MauiClient.Toolkit;
using System.Net;
using System.Reactive.Linq;
using System.Windows.Input;

namespace SlowUdpPipe.MauiClient.ViewModels;

public class TunnelEditViewModel : ObservableModel
{
  private readonly IPagesController p_pagesController;
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private string p_name;
  private string p_localAddress;
  private string p_remoteAddress;
  private EncryptionAlgorithm p_encryptionAlgo;
  private string p_key;
  private bool p_enabled;

  public TunnelEditViewModel(TunnelsConfEntry _tunnelEntryEntry)
  {
    p_pagesController = Container.Locate<IPagesController>();
    p_tunnelsConfCtrl = Container.Locate<ITunnelsConfCtrl>();

    Guid = _tunnelEntryEntry.Guid;

    NameCommand = new Command(OnName);
    RemoteAddressCommand = new Command(OnRemoteAddress);
    LocalAddressCommand = new Command(OnLocalAddress);
    EncryptionCommand = new Command(OnEncryption);
    KeyCommand = new Command(OnKey);

    p_name = _tunnelEntryEntry.Name;
    p_localAddress = _tunnelEntryEntry.LocalAddress;
    p_remoteAddress = _tunnelEntryEntry.RemoteAddress;
    p_encryptionAlgo = _tunnelEntryEntry.EncryptionAlgo;
    p_key = _tunnelEntryEntry.Key;
    p_enabled = _tunnelEntryEntry.Enabled;

    OnPropertyChanged(nameof(Name));
    OnPropertyChanged(nameof(LocalAddress));
    OnPropertyChanged(nameof(RemoteAddress));
    OnPropertyChanged(nameof(EncryptionAlgo));
    OnPropertyChanged(nameof(Key));
    OnPropertyChanged(nameof(Enabled));
  }

  public Guid Guid { get; }
  public Color DeleteBtnColor => Data.Consts.COLOR_DELETE_TUNNEL;
  public string DeleteBtnText => "Delete tunnel";

  public string Name
  {
    get => p_name;
    set
    {
      if (value.Equals( p_name))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_name = value;
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(Name));
    }
  }
  public string LocalAddress
  {
    get => p_localAddress;
    set
    {
      if (value.Equals(p_localAddress))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_localAddress = value;
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(LocalAddress));
    }
  }
  public string RemoteAddress
  {
    get => p_remoteAddress;
    set
    {
      if (value.Equals(p_remoteAddress))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_remoteAddress = value;
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(RemoteAddress));
    }
  }
  public string EncryptionAlgo
  {
    get => Common.Data.Consts.ENCRYPTION_ALG_SLUG[p_encryptionAlgo];
    set
    {
      if (value.Equals(Common.Data.Consts.ENCRYPTION_ALG_SLUG[p_encryptionAlgo]))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_encryptionAlgo = Common.Data.Consts.ENCRYPTION_ALG_SLUG_REVERSE[value];
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(EncryptionAlgo));
    }
  }
  public string Key => p_key;
  public string CipheredKey
  {
    get
    {
      var key = p_key;
      if (key.Length < 8)
        return new string('*', key.Length);
      else
        return $"{new string('*', key.Length - 4)}{key[^4..]}";
    }
    set
    {
      if (value.Equals(p_key))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_key = value;
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(Key));
      OnPropertyChanged(nameof(CipheredKey));
    }
  }
  public bool Enabled
  {
    get => p_enabled;
    set
    {
      if (value.Equals(p_enabled))
        return;

      p_enabled = value;
      p_tunnelsConfCtrl.UpdateTunnel(this);
      OnPropertyChanged(nameof(Enabled));
    }
  }

  public ICommand NameCommand { get; }
  public ICommand RemoteAddressCommand { get; }
  public ICommand LocalAddressCommand { get; }
  public ICommand EncryptionCommand { get; }
  public ICommand KeyCommand { get; }

  private async void OnName(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var dialogResult = await currentPage.DisplayPromptAsync(
        "Name:",
        null,
        "Save",
        placeholder: "",
        initialValue: Name,
        keyboard: Keyboard.Plain);

    if (dialogResult == null)
      return;

    Name = dialogResult;
  }

  private async void OnRemoteAddress(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var address = await currentPage.DisplayPromptAsync(
        "Remote Address:",
        null,
        "Save",
        placeholder: "<ip-address>:<port>",
        initialValue: RemoteAddress,
        keyboard: Keyboard.Url);

    if (address == null)
      return;
    if (!IPEndPoint.TryParse(address, out _))
    {
      await currentPage.DisplayAlert("The address should be in the format 'ip:port'", "For example, '123.123.123.123:12345'", "Okay");
      return;
    }

    RemoteAddress = address;
  }

  private async void OnLocalAddress(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var address = await currentPage.DisplayPromptAsync(
        "Local Address:",
        null,
        "Save",
        placeholder: "<ip-address>:<port>",
        initialValue: LocalAddress,
        keyboard: Keyboard.Url);

    if (address == null)
      return;

    if (!IPEndPoint.TryParse(address, out _))
    {
      await currentPage.DisplayAlert("The address should be in the format 'ip:port'", "For example, '123.123.123.123:12345'", "Okay");
      return;
    }

    LocalAddress = address;
  }

  private async void OnEncryption(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var result = await currentPage.DisplayActionSheet("Please select algorithm:", null, null, Common.Data.Consts.ENCRYPTION_ALG_SLUG_REVERSE.Keys.ToArray());
    if (result == null)
      return;

    EncryptionAlgo = result;
  }

  private async void OnKey(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var text = await currentPage.DisplayPromptAsync(
        "Key:",
        null,
        "Save",
        placeholder: "key",
        initialValue: string.Empty,
        keyboard: Keyboard.Plain);

    if (text == null)
      return;
    if (text.IsNullOrWhiteSpace())
    {
      await currentPage.DisplayAlert("Empty key is not allowed", string.Empty, "Okay");
      return;
    }

    CipheredKey = text;
  }

}
