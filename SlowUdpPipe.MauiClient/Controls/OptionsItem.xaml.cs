using System.Windows.Input;

namespace SlowUdpPipe.MauiClient.Controls;

public partial class OptionsItem : ContentView
{
  public OptionsItem()
  {
    InitializeComponent();
  }

  public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(OptionsItem), propertyChanged: (_bindable, _old, _new) =>
  {
    var control = (OptionsItem)_bindable;
    control.TitleLabel.Text = _new as string;
  });

  public static readonly BindableProperty DetailsTextProperty = BindableProperty.Create(nameof(DetailsText), typeof(string), typeof(OptionsItem), propertyChanged: (_bindable, _old, _new) =>
  {
    var control = (OptionsItem)_bindable;
    control.DetailsTextLabel.Text = _new as string;
  });

  public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(OptionsItem), propertyChanged: (_bindable, _old, _new) =>
  {
    var control = (OptionsItem)_bindable;
    control.TapCommandHandler.Command = _new as ICommand;
  });

  public static readonly BindableProperty ShowBottomLineProperty = BindableProperty.Create(nameof(ShowBottomLine), typeof(bool), typeof(OptionsItem), propertyChanged: (_bindable, _old, _new) =>
  {
    var control = (OptionsItem)_bindable;
    control.BottomLine.IsVisible = (bool)_new;
  }, defaultValue: true);

  public static readonly BindableProperty ShowSwitchProperty = BindableProperty.Create(nameof(ShowSwitch), typeof(bool), typeof(OptionsItem), propertyChanged: (_bindable, _old, _new) =>
  {
    var control = (OptionsItem)_bindable;
    control.Switch.IsVisible = (bool)_new;
  });

  public static readonly BindableProperty SwitchTappedProperty = BindableProperty.Create(nameof(SwitchTapped), typeof(ICommand), typeof(OptionsItem));


  public string Title
  {
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
  }

  public string DetailsText
  {
    get=> (string)GetValue(DetailsTextProperty);
    set => SetValue(DetailsTextProperty, value);
  }

  public ICommand TapCommand
  {
    get => (ICommand)GetValue(TapCommandProperty);
    set => SetValue(TapCommandProperty, value);
  }

  public bool ShowBottomLine
  {
    get => (bool)GetValue(ShowBottomLineProperty);
    set => SetValue(ShowBottomLineProperty, value);
  }

  public bool ShowSwitch
  {
    get => (bool)GetValue(ShowSwitchProperty);
    set => SetValue(ShowSwitchProperty, value);
  }

  public bool SwitchIsToggled
  {
    get => Switch.IsToggled;
    set => Switch.IsToggled = value;
  }

  public ICommand SwitchTapped
  {
    get => (ICommand)GetValue(SwitchTappedProperty);
    set => SetValue(SwitchTappedProperty, value);
  }

  private void Switch_Toggled(object _sender, ToggledEventArgs _e) => SwitchTapped.Execute(_e.Value);

}