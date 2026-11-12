using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
internal static class ReleaseScreenshotSession {
 internal static void Run(string language="en-US"){
  ScreenWisp.Localization.Localizer.Current.Apply(language);
  // Real windows using production UI classes, neutral settings and no user desktop content.
  var backdrop=new Window{Title="ScreenWisp screenshot backdrop",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Width=1100,Height=850,Left=20,Top=20,Background=new SolidColorBrush(Color.FromRgb(232,235,241)),Topmost=true,Content=new TextBlock{Text="ScreenWisp",FontSize=20,Margin=new Thickness(24),Foreground=Brushes.SlateGray}};backdrop.Show();
  var settings=new Settings{Language=language};var window=new SettingsWindow(settings,(_,_)=>{}){WindowStartupLocation=WindowStartupLocation.Manual,Left=100,Top=70,Topmost=true};window.Show();window.Activate();
  var popup=new BrushPopup(()=>settings,s=>settings=s,()=>{}){ShowInTaskbar=true};popup.ShowAt(1000,720);ExposeForCapture();
  // Capture fixture only: expose the production tool window to window-capture APIs.
  // Product ShowAt/nonactivation behavior remains unchanged and is separately tested.
  void ExposeForCapture(){var h=new System.Windows.Interop.WindowInteropHelper(popup).Handle;Win32.SetWindowLongPtr(h,-20,new IntPtr(Win32.GetWindowLongPtr(h,-20).ToInt64()&~0x08000080L));}
  var reveal=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};reveal.Tick+=(_,_)=>{if(!popup.IsVisible){popup.ShowAt(1000,460);ExposeForCapture();}};reveal.Start();
  var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(300)};
  timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();
  Console.WriteLine("SCREENSHOT WINDOWS READY (300 seconds)");Dispatcher.PushFrame(frame);reveal.Stop();popup.Close();window.Close();backdrop.Close();
 }
}
