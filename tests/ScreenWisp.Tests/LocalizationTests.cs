using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Localization;
using ScreenWisp.Windows;
internal static class LocalizationTests {
 internal static IEnumerable<T> Descendants<T>(DependencyObject node) where T:DependencyObject {
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var child=VisualTreeHelper.GetChild(node,i);if(child is T typed)yield return typed;foreach(var nested in Descendants<T>(child))yield return nested;}
 }
 static T Named<T>(DependencyObject node,string name) where T:FrameworkElement=>Descendants<T>(node).Single(e=>e.Name==name);
 static void Pump()=>Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
 internal static void Run(Action<bool,string> check,string? output=null){
  var l=Localizer.Current;string previous=l.Preference;
  var dir=Path.Combine(Path.GetTempPath(),"ScreenWisp.Localization-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try {
   check(Localizer.Resolve("auto",CultureInfo.GetCultureInfo("ru-RU")).Name=="ru-RU","Automatic resolves Russian Windows");
   check(Localizer.Resolve("auto",CultureInfo.GetCultureInfo("en-GB")).Name=="en-US","Automatic resolves English Windows");
   foreach(string other in new[]{"de-DE","zh-CN","fr-FR","ja-JP"})check(Localizer.Resolve("auto",CultureInfo.GetCultureInfo(other)).Name=="en-US","Automatic falls back to English for "+other);
   check(Localizer.Resolve("ru-RU",CultureInfo.GetCultureInfo("en-US")).Name=="ru-RU"&&Localizer.Resolve("en-US",CultureInfo.GetCultureInfo("ru-RU")).Name=="en-US","Explicit language overrides Windows");
   var resources=new ResourceManager("ScreenWisp.Localization.Strings",typeof(Localizer).Assembly);
   string[] Keys(string culture)=>resources.GetResourceSet(CultureInfo.GetCultureInfo(culture),true,false)!.Cast<DictionaryEntry>().Select(p=>(string)p.Key).OrderBy(k=>k).ToArray();
   var english=Keys("en-US");var russian=Keys("ru-RU");
   check(english.Length>=50&&english.SequenceEqual(russian),"English/Russian resource key parity");
   foreach(string key in english){
    var en=Localizer.Get(key,CultureInfo.GetCultureInfo("en-US"));var ru=Localizer.Get(key,CultureInfo.GetCultureInfo("ru-RU"));
    check(!string.IsNullOrWhiteSpace(en)&&!string.IsNullOrWhiteSpace(ru)&&!en.Contains('\uFFFD')&&!ru.Contains('\uFFFD'),"Nonempty UTF-8 translations: "+key);
    check(System.Text.RegularExpressions.Regex.Matches(en,@"\{\d+(?::[^}]+)?\}").Select(m=>m.Value).SequenceEqual(System.Text.RegularExpressions.Regex.Matches(ru,@"\{\d+(?::[^}]+)?\}").Select(m=>m.Value)),"Matching format placeholders: "+key);
   }
   check(Localizer.Get("Settings",CultureInfo.GetCultureInfo("fr-FR"))=="Settings","ResourceManager neutral English fallback");
   var store=new SettingsStore(Path.Combine(dir,"settings.json"));
   foreach(bool? startup in new bool?[]{true,false,null}){
    var original=new Settings{Toggle=new(7,0x73),Clear=new(7,0x74),Undo=new(7,0x75),Hold=new(6,0x76),TemporaryUndo=new(4,0x5A),AutoStart=startup,Color=PenColor.Purple,Width=8};
    var old=JsonNode.Parse(JsonSerializer.Serialize(original))!;old.AsObject().Remove("Language");File.WriteAllText(Path.Combine(dir,"settings.json"),old.ToJsonString());
    check(store.Load()==original,"Legacy RC2 profile keeps every setting; autostart="+startup);
    foreach(string language in new[]{"auto","en-US","ru-RU"}){store.Save(original with{Language=language});check(store.Load()==original with{Language=language},"Language roundtrip preserves profile: "+language+"/"+startup);}
    old["Language"]="unknown-language";File.WriteAllText(Path.Combine(dir,"settings.json"),old.ToJsonString());check(store.Load()==original,"Unknown language falls back without resetting profile");
   }
   var culture=CultureInfo.CurrentCulture;var format=new Settings().Toggle.ToString();
   l.Apply("en-US");Exception? failure=null;try{Hotkey.Parse("D");}catch(ArgumentException ex){failure=ex;}
   check(failure is not null&&failure.Message=="A modifier and a key are required.","English shortcut validation");
   l.Apply("ru-RU");check(failure!.Message=="Нужны модификатор и клавиша.","Existing validation exception translates live");
   l.Apply("en-US");check(Localizer.Error(new LocalizedWin32Exception(1409,"ErrorHotkeyBusy",new Hotkey(6,0x44))).Contains("Ctrl+Shift+D"),"Native conflict message retains invariant shortcut");
   check(!Localizer.Error(new IOException("raw unlocalized detail")).Contains("raw unlocalized"),"User error does not leak untranslated native exception text");
   var model=new Settings{Language="en-US",AutoStart=false};store.Save(model);
   using var menu=new TrayMenuController(()=>{},()=>{});
   var popup=new BrushPopup(()=>model,next=>model=next,()=>{});
   var window=new SettingsWindow(model,(next,_)=>model=next,preference=>{model=model with{Language=preference};store.Save(model);});
   try{
    window.Show();popup.Show();window.UpdateLayout();popup.UpdateLayout();Pump();
    var language=Named<ComboBox>(window,"LanguageSelector");var toggle=Named<TextBox>(window,"PersistentDrawingField");toggle.Text="Ctrl+Shift+F9";
    language.SelectedValue="ru-RU";Pump();
    check(window.Title=="ScreenWisp — Настройки"&&menu.Menu.Items[0].Text=="Настройки"&&popup.Title=="ScreenWisp — Кисть","Open settings, popup and native tray menu switch immediately");
    check(store.Load().Language=="ru-RU"&&store.Load().Toggle==new Settings().Toggle&&toggle.Text=="Ctrl+Shift+F9","Immediate language save preserves persisted shortcuts and unsaved text edits");
    check(((Button)((StackPanel)popup.FindName("Swatches")).Children[0]).ToolTip?.ToString()=="Белый","Swatch tooltip switches immediately");
    language.SelectedValue="en-US";Pump();check(window.Title=="ScreenWisp — Settings"&&menu.Menu.Items[1].Text=="Exit"&&popup.Title=="ScreenWisp — Brush","Switch back without recreating windows");
    check(((Button)((StackPanel)popup.FindName("Swatches")).Children[0]).ToolTip?.ToString()=="White","English color names without enum rename");
    check(Enum.GetNames<PenColor>().SequenceEqual(new[]{"Red","Blue","Green","White","Purple","Yellow"})&&new Settings().Toggle.ToString()==format&&CultureInfo.CurrentCulture==culture,"Enums, shortcut format and numeric culture remain invariant");
    // Rendering/measurement at each scale is distinct from changing Windows monitor DPI.
    foreach(string lang in new[]{"en-US","ru-RU"}){
     language.SelectedValue=lang;Pump();
     foreach(double scale in new[]{1.0,1.25,1.5,2.0}){
      window.UpdateLayout();popup.Measure(new Size(popup.Width,popup.Height));popup.Arrange(new Rect(0,0,popup.Width,popup.Height));popup.UpdateLayout();
      foreach(var text in Descendants<TextBlock>(popup).Where(t=>t.Text.Length>0)){
       var measured=new FormattedText(text.Text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(text.FontFamily,text.FontStyle,text.FontWeight,text.FontStretch),text.FontSize,Brushes.Black,scale);
       check(measured.WidthIncludingTrailingWhitespace<=text.ActualWidth+.5,"Popup label fits "+lang+"/"+scale+": "+text.Text);
      }
      var scroll=(ScrollViewer)window.Content;scroll.ScrollToEnd();window.UpdateLayout();
      check(scroll.ScrollableWidth==0&&Named<Button>(window,"SaveSettings").ActualWidth>=50,"Settings horizontal fit and accessible Save "+lang+"/"+scale);
      if(output is not null){
       Directory.CreateDirectory(output);
       Render((Visual)window.Content,window.ActualWidth-16,window.ActualHeight-39,scale,Path.Combine(output,"settings-render-"+lang+"-"+(int)(scale*100)+".png"));
       Render(popup,popup.Width,popup.Height,scale,Path.Combine(output,"popup-render-"+lang+"-"+(int)(scale*100)+".png"));
      }
      scroll.ScrollToHome();
     }
    }
    language.SelectedValue="en-US";toggle.Text="D";Named<Button>(window,"SaveSettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
    check(Named<TextBlock>(window,"ValidationStatus").Text=="A modifier and a key are required.","Settings displays localized validation");
    // Save valid edited settings, including the current language and unchanged values.
    toggle.Text="Ctrl+Shift+F9";Named<Button>(window,"SaveSettings").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    check(model.Toggle==Hotkey.Parse("Ctrl+Shift+F9")&&model.Language=="en-US"&&model.AutoStart==false,"Save preserves language and applies edits");
   }finally{window.Close();popup.Close();}
   // Failure to persist a language choice rolls back the selection and keeps a usable UI.
   l.Apply("en-US");var failed=new SettingsWindow(new Settings{Language="en-US"},(_,_)=>{},_=>throw new IOException("test failure"));
   try{failed.Show();failed.UpdateLayout();Named<ComboBox>(failed,"LanguageSelector").SelectedValue="ru-RU";Pump();check(l.Preference=="en-US"&&(string)Named<ComboBox>(failed,"LanguageSelector").SelectedValue=="en-US"&&Named<TextBlock>(failed,"ValidationStatus").Text.Contains("operation failed"),"Failed language save rolls back without changing other settings");}finally{failed.Close();}
  }finally{l.Apply(previous);Directory.Delete(dir,true);}
 }
 static void Render(Visual visual,double width,double height,double scale,string file){
  var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){dc.DrawRectangle(Brushes.White,null,new Rect(0,0,width,height));dc.DrawRectangle(new VisualBrush(visual){Stretch=Stretch.None,AlignmentX=AlignmentX.Left,AlignmentY=AlignmentY.Top},null,new Rect(0,0,width,height));}
  var bitmap=new RenderTargetBitmap((int)Math.Ceiling(width*scale),(int)Math.Ceiling(height*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(drawing);
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(file);encoder.Save(stream);
 }
}
