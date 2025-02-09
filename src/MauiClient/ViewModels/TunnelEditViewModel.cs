using Ax.Fw.Extensions;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using CloakTunnel.MauiClient.Data;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Toolkit;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CloakTunnel.MauiClient.ViewModels;

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
  public Color DeleteBtnColor => Data.AppConsts.COLOR_DELETE_TUNNEL;
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
    get => EncryptionToolkit.ENCRYPTION_ALG_SLUG[p_encryptionAlgo];
    set
    {
      if (value.Equals(EncryptionToolkit.ENCRYPTION_ALG_SLUG[p_encryptionAlgo]))
        return;

      if (value.IsNullOrWhiteSpace())
        return;

      p_encryptionAlgo = EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE[value];
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
  public string SemanticText => $"Tunnel '{Name}' (Id: '{Guid}')";

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
        "Tunnel's name:",
        null,
        "Save",
        placeholder: "",
        initialValue: Name,
        keyboard: Keyboard.Plain);

    if (dialogResult == null)
      return;

    Name = dialogResult;
  }

  private async void OnLocalAddress(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var rawUri = await currentPage.DisplayPromptAsync(
        "Bind (local) URI:",
        null,
        "Save",
        placeholder: "udp://<ip-address>:<port>",
        initialValue: LocalAddress,
        keyboard: Keyboard.Url);

    if (rawUri == null)
      return;
    if (!UriToolkit.CheckUdpUri(rawUri, out _))
    {
      await currentPage.DisplayAlert(
        "Incorrect URI format",
        "The address should be in the format 'udp://<domain/ip>:<port>; for example, 'udp://123.123.123.123:12345'", 
        "Okay");

      return;
    }

    LocalAddress = rawUri;
  }

  private async void OnRemoteAddress(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var rawUri = await currentPage.DisplayPromptAsync(
        "Forward (server) URI:",
        null,
        "Save",
        placeholder: "<udp/ws/wss>://<ip-address>:<port>",
        initialValue: RemoteAddress,
        keyboard: Keyboard.Url);

    if (rawUri == null)
      return;
    if (!UriToolkit.CheckUdpOrWsOrWssUri(rawUri, out _))
    {
      await currentPage.DisplayAlert(
        "Incorrect URI format",
        "The address should be in the format '<scheme>://<domain/ip>:<port>'; for example, 'udp://123.123.123.123:12345' or 'wss://example.com:8088/endpoint'", 
        "Okay");

      return;
    }

    RemoteAddress = rawUri;
  }

  private async void OnEncryption(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var result = await currentPage.DisplayActionSheet("Please select encryption:", null, null, EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE.Keys.ToArray());
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
