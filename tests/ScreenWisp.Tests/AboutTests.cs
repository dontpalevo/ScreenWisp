using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using ScreenWisp.Infrastructure;
using ScreenWisp.Localization;
using ScreenWisp.Windows;
internal static class AboutTests {
 internal static void Run(Action<bool,string> check){
  var locale=Localizer.Current;string previous=locale.Preference;
  var assembly=typeof(Localizer).Assembly;
  check(assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company=="dontpalevo","App company metadata is dontpalevo");
  check(assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product=="ScreenWisp","App product metadata is ScreenWisp");
  check(ProductInfo.Version==assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,"About version comes from app assembly");
  var copyright=assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright;
  check(copyright.Contains("dontpalevo")&&copyright.Contains("atman-33"),"Metadata distinguishes ScreenWisp and upstream authors");
  var window=new SettingsWindow(new Settings(),(_,_)=>{});
  try{
   window.Show();window.UpdateLayout();
   var about=LocalizationTests.Descendants<AboutSection>(window).Single();
   var texts=LocalizationTests.Descendants<TextBlock>(about).ToArray();
   TextBlock Named(string n)=>texts.Single(t=>t.Name==n);
   foreach(string language in new[]{"en-US","ru-RU"}){
    locale.Apply(language);GC.Collect();GC.WaitForPendingFinalizers();
    Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
    window.UpdateLayout();
    check(Named("AboutHeading").Text==locale["About"],"About heading translates live "+language);
    check(Named("AboutDeveloper").Text==locale["AboutDeveloper"],"Developer attribution translates "+language);
    check(Named("AboutUpstream").Text==locale["AboutUpstream"],"Desktop Ink credit translates "+language);
    check(Named("AboutVersion").Text==locale.Format("AboutVersion",ProductInfo.Version),"About version translates after GC "+language);
    check(Named("AboutLicense").Text==locale["AboutLicense"],"MIT label translates "+language);
    var link=texts.SelectMany(t=>t.Inlines.OfType<Hyperlink>()).Single();
    check(link.NavigateUri==new Uri(ProductInfo.GitHub)&&link.Inlines.OfType<Run>().Single().Text==ProductInfo.GitHub,"GitHub target is explicit profile; no auto navigation "+language);
    var scroll=(ScrollViewer)window.Content;scroll.ScrollToEnd();window.UpdateLayout();
    check(about.ActualWidth>350&&scroll.ScrollableWidth==0,"About fits settings scroll width "+language);
    string path=Path.Combine(Path.GetTempPath(),"ScreenWisp-about-"+language+".png");
    var visual=(Visual)window.Content;
    var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){dc.DrawRectangle(Brushes.White,null,new Rect(0,0,480,850));dc.DrawRectangle(new VisualBrush(visual),null,new Rect(0,0,480,850));}
    var image=new RenderTargetBitmap(480,850,96,96,PixelFormats.Pbgra32);image.Render(drawing);
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(path);encoder.Save(file);
   }
  }finally{window.Close();locale.Apply(previous);}
 }
}
