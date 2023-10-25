using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using System.Net;
using System.Reactive.Linq;
using System.Windows.Input;
using static SlowUdpPipe.MauiClient.Data.Consts;

namespace SlowUdpPipe.MauiClient.ViewModels;

internal class MainPageViewModel : BaseViewModel
{
  private readonly IPreferencesStorage p_preferences;
  private readonly IPagesController p_pagesController;
  private string p_title;
  private string? p_remoteAddress;
  private string? p_localAddress;
  private EncryptionAlgorithm p_cipher;
  private string? p_trucatedKey;
  private string? p_startStopBtnText;
  private Color? p_startStopBtnColor;

  public MainPageViewModel()
  {
    p_preferences = Container.Locate<IPreferencesStorage>();
    p_pagesController = Container.Locate<IPagesController>();

    p_title = "Slow Udp Pipe";

    RemoteAddressCommand = new Command(OnRemoteAddress);
    LocalAddressCommand = new Command(OnLocalAddress);
    CipherCommand = new Command(OnCipher);
    KeyCommand = new Command(OnKey);
    UpTunnelOnAppStartupCommand = new Command(OnUpTunnelOnAppStartup);

    var lifetime = Container.Locate<IReadOnlyLifetime>();
    p_preferences.PreferencesChanged
      .StartWithDefault()
      .Subscribe(_ =>
      {
        var key = p_preferences.GetValueOrDefault<string>(PREF_DB_KEY);
        if (key == null)
          SetProperty(ref p_trucatedKey, "", nameof(Key));
        else if (key.Length < 8)
          SetProperty(ref p_trucatedKey, new string('*', key.Length), nameof(Key));
        else
          SetProperty(ref p_trucatedKey, $"{new string('*', key.Length - 4)}{key[^4..]}", nameof(Key));

        SetProperty(ref p_remoteAddress, p_preferences.GetValueOrDefault<string>(PREF_DB_REMOTE), nameof(RemoteAddress));
        SetProperty(ref p_localAddress, p_preferences.GetValueOrDefault<string>(PREF_DB_LOCAL), nameof(LocalAddress));
        SetProperty(ref p_cipher, p_preferences.GetValueOrDefault<EncryptionAlgorithm>(PREF_DB_CIPHER), nameof(Cipher));
      }, lifetime);
  }

  public string Title { get => p_title; set => SetProperty(ref p_title, value); }

  public string? RemoteAddress
  {
    get => p_remoteAddress;
    set => p_preferences.SetValue(PREF_DB_REMOTE, value);
  }
  public string? LocalAddress
  {
    get => p_localAddress;
    set => p_preferences.SetValue(PREF_DB_LOCAL, value);
  }
  public EncryptionAlgorithm Cipher
  {
    get => p_cipher;
    set => p_preferences.SetValue(PREF_DB_CIPHER, value);
  }
  public string? Key
  {
    get => p_trucatedKey ?? string.Empty;
    set => p_preferences.SetValue(PREF_DB_KEY, value);
  }
  public string StartStopBtnText
  {
    get => p_startStopBtnText ?? "";
    set => SetProperty(ref p_startStopBtnText, value, nameof(StartStopBtnText));
  }
  public Color StartStopBtnColor
  {
    get => p_startStopBtnColor ?? COLOR_UP_TUNNEL_OFF;
    set => SetProperty(ref p_startStopBtnColor, value, nameof(StartStopBtnColor));
  }
  
  public ICommand RemoteAddressCommand { get; }
  public ICommand LocalAddressCommand { get; }
  public ICommand CipherCommand { get; }
  public ICommand KeyCommand { get; }
  public ICommand UpTunnelOnAppStartupCommand { get; }

  private async void OnRemoteAddress(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var address = await currentPage.DisplayPromptAsync(
        "Remote Address",
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
        "Local Address",
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

  private async void OnCipher(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var options = new Dictionary<string, EncryptionAlgorithm>
    {
      { "aes-128", EncryptionAlgorithm.Aes128 },
      { "aes-256", EncryptionAlgorithm.Aes256 },
      { "aes-gcm-128", EncryptionAlgorithm.AesGcm128 },
      { "aes-gcm-256", EncryptionAlgorithm.AesGcm256 },
      { "chacha20-poly1305", EncryptionAlgorithm.ChaCha20Poly1305 },
      { "xor (may be detectable)", EncryptionAlgorithm.Xor }
    };

    var result = await currentPage.DisplayActionSheet("Please select cypher", null, null, options.Keys.ToArray());
    if (result == null)
      return;

    Cipher = options[result];
  }

  private async void OnKey(object _arg)
  {
    var currentPage = p_pagesController.CurrentPage;
    if (currentPage == null)
      return;

    var text = await currentPage.DisplayPromptAsync(
        "Key",
        null,
        "Save",
        placeholder: "key",
        initialValue: string.Empty,
        keyboard: Keyboard.Plain);

    if (text == null)
      return;
    if (text.Length == 0)
    {
      await currentPage.DisplayAlert("Empty key is not allowed", string.Empty, "Okay");
      return;
    }

    Key = text;
  }

  private void OnUpTunnelOnAppStartup(object _arg)
  {
    if (_arg is not bool toggled)
      return;

    p_preferences.SetValue(PREF_DB_UP_TUNNEL_ON_APP_STARTUP, toggled);
  }

}
