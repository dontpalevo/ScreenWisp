using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Data;
namespace ScreenWisp.Localization;

// ResourceManager supplies neutral English fallback; user preference never changes
// CurrentCulture, enum names, shortcut parsing, or serialized numeric values.
public sealed class Localizer : INotifyPropertyChanged {
 public static Localizer Current {get;}=new();
 static readonly ResourceManager Resources=new("ScreenWisp.Localization.Strings",typeof(Localizer).Assembly);
 readonly CultureInfo windowsCulture=CultureInfo.CurrentUICulture;
 public event PropertyChangedEventHandler? PropertyChanged;
 public event Action? Changed;
 public string Preference {get;private set;}="auto";
 public CultureInfo Culture {get;private set;}=Resolve("auto",CultureInfo.CurrentUICulture);
 public string this[string key]=>Get(key,Culture);
 public static string Normalize(string? preference)=>preference is "en-US" or "ru-RU"?preference:"auto";
 public static CultureInfo Resolve(string? preference,CultureInfo windows)=>CultureInfo.GetCultureInfo(Normalize(preference) switch {
  "en-US"=>"en-US","ru-RU"=>"ru-RU",_=>windows.TwoLetterISOLanguageName=="ru"?"ru-RU":"en-US"});
 public static string Get(string key,CultureInfo culture)=>Resources.GetString(key,culture)??throw new MissingManifestResourceException("Missing UI resource: "+key);
 public string Format(string key,params object[] args)=>string.Format(Culture,this[key],args);
 public void Apply(string? preference){
  Preference=Normalize(preference);Culture=Resolve(Preference,windowsCulture);
  PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(string.Empty));Changed?.Invoke();
 }
 public static void Bind(DependencyObject target,DependencyProperty property,string key)=>BindingOperations.SetBinding(target,property,new Binding("["+key+"]"){Source=Current,Mode=BindingMode.OneWay});
 public static string Error(Exception exception)=>exception is LocalizedArgumentException argument?argument.Message:
  exception is LocalizedWin32Exception native?native.Message:
  exception is LocalizedOperationException operation?operation.Message:
  Current.Format("GenericError","0x"+exception.HResult.ToString("X8",CultureInfo.InvariantCulture));
}
internal sealed class LocalizedArgumentException(string resourceKey) : ArgumentException {
 public override string Message=>Localizer.Current[resourceKey];
}
internal sealed class LocalizedWin32Exception(int code,string resourceKey,params object[] arguments) : Win32Exception(code) {
 public override string Message=>Localizer.Current.Format(resourceKey,arguments);
}
internal sealed class LocalizedOperationException(string resourceKey) : InvalidOperationException {
 public override string Message=>Localizer.Current[resourceKey];
}
