using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Localization;
namespace ScreenWisp.Windows;
internal sealed class SettingsWindow : Window {
 internal SettingsWindow(Settings settings,Action<Settings,bool> save,Action<string>? changeLanguage=null) {
  Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/ScreenWisp;component/Assets/screenwisp.ico"));
  Localizer.Bind(this,TitleProperty,"SettingsTitle");
  Width=500;Height=Math.Min(900,SystemParameters.WorkArea.Height-40);MinWidth=360;ResizeMode=ResizeMode.CanMinimize;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  var panel=new StackPanel{Margin=new Thickness(24)};Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  TextBlock Label(string key,Thickness? margin=null){var text=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=margin??new Thickness(0)};Localizer.Bind(text,TextBlock.TextProperty,key);panel.Children.Add(text);return text;}
  ComboBoxItem Choice(string key,object value){var label=new TextBlock();Localizer.Bind(label,TextBlock.TextProperty,key);return new ComboBoxItem{Tag=value,Content=label};}
  panel.Children.Add(new TextBlock{Text="ScreenWisp",FontSize=26,FontWeight=FontWeights.SemiBold});
  Label("Language",new Thickness(0,12,0,4));
  var language=new ComboBox{Name="LanguageSelector",SelectedValuePath="Tag",MinHeight=28};
  language.Items.Add(Choice("LanguageAuto","auto"));language.Items.Add(Choice("LanguageEnglish","en-US"));language.Items.Add(Choice("LanguageRussian","ru-RU"));language.SelectedValue=Localizer.Normalize(settings.Language);panel.Children.Add(language);
  Label("LanguageHint",new Thickness(0,4,0,4)).FontSize=11;
  Label("GestureHelp",new Thickness(0,10,0,0));Label("HotkeyHelp",new Thickness(0,8,0,12));
  TextBox Field(string key,string value){Label(key);var box=new TextBox{Name=key+"Field",Text=value,Margin=new Thickness(0,4,0,8),Padding=new Thickness(5)};Localizer.Bind(box,System.Windows.Automation.AutomationProperties.NameProperty,key);panel.Children.Add(box);return box;}
  var toggle=Field("PersistentDrawing",settings.Toggle.ToString());
  var clear=Field("ClearScreens",settings.Clear.ToString());
  var undo=Field("UndoStroke",settings.Undo.ToString());
  var tempUndo=Field("TemporaryUndo",settings.TemporaryUndo.ToString());
  Label("UndoHint",new Thickness(0,0,0,8));
  var hold=Field("AlternativeHold",settings.Hold.ToString());
  var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition());panel.Children.Add(row);
  var color=new ComboBox{Name="ColorSelector",SelectedValuePath="Tag",Margin=new Thickness(0,0,12,8),MinHeight=28};
  foreach(var option in Enum.GetValues<PenColor>())color.Items.Add(Choice("Color"+option,option));
  color.SelectedValue=settings.Color;Localizer.Bind(color,ToolTipProperty,"Color");Localizer.Bind(color,System.Windows.Automation.AutomationProperties.NameProperty,"Color");row.Children.Add(color);
  var width=new ComboBox{Name="WidthSelector",ItemsSource=PenOptions.Widths,SelectedItem=settings.Width,Margin=new Thickness(0,0,0,8),MinHeight=28};
  Localizer.Bind(width,ToolTipProperty,"WidthHint");Localizer.Bind(width,System.Windows.Automation.AutomationProperties.NameProperty,"Thickness");Grid.SetColumn(width,1);row.Children.Add(width);
  var startup=new CheckBox{Name="AutostartToggle",IsChecked=settings.AutoStart ?? StartupService.Enabled,Margin=new Thickness(0,8,0,12)};
  var startupLabel=new TextBlock{TextWrapping=TextWrapping.Wrap};Localizer.Bind(startupLabel,TextBlock.TextProperty,"Autostart");startup.Content=startupLabel;panel.Children.Add(startup);
  var status=new TextBlock{Name="ValidationStatus",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Firebrick};panel.Children.Add(status);
  Exception? lastError=null;
  void TranslateStatus(){if(lastError is not null)status.Text=Localizer.Error(lastError);}
  Localizer.Current.Changed+=TranslateStatus;Closed+=(_,_)=>Localizer.Current.Changed-=TranslateStatus;
  var button=new Button{Name="SaveSettings",Padding=new Thickness(18,7,18,7),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,0,0)};Localizer.Bind(button,ContentControl.ContentProperty,"Save");panel.Children.Add(button);
  panel.Children.Add(new AboutSection());
  bool languageBusy=false;string committedLanguage=Localizer.Normalize(settings.Language);
  language.SelectionChanged+=(_,_)=>{
   if(languageBusy||language.SelectedValue is not string preference)return;
   try{changeLanguage?.Invoke(preference);Localizer.Current.Apply(preference);committedLanguage=preference;lastError=null;status.Text="";}
   catch(Exception ex){AppLog.Error("Language preference save failed",ex);lastError=ex;languageBusy=true;language.SelectedValue=committedLanguage;languageBusy=false;TranslateStatus();}
  };
  button.Click+=(_,_)=>{try{
   var next=settings with{Language=(string)language.SelectedValue,Toggle=Hotkey.Parse(toggle.Text),Clear=Hotkey.Parse(clear.Text),Undo=Hotkey.Parse(undo.Text),Hold=Hotkey.Parse(hold.Text),TemporaryUndo=Hotkey.Parse(tempUndo.Text,true),Color=(PenColor)color.SelectedValue,Width=(double)width.SelectedItem};
   next.Validate();save(next,startup.IsChecked==true);Close();
  }catch(Exception ex){AppLog.Error("Settings apply failed",ex);lastError=ex;TranslateStatus();}};
 }
}
