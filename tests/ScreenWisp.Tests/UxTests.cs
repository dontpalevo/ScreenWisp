using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
using Forms=System.Windows.Forms;
internal static class UxTests {
 internal static void Run(Action<bool,string> check){
  check(StartupRegistration.ResolvePreference(null,false,false),"first launch defaults autostart on");
  check(!StartupRegistration.ResolvePreference(false,false,false),"explicit autostart off wins");
  check(!StartupRegistration.ResolvePreference(null,true,false),"migration preserves previously disabled autostart");
  check(StartupRegistration.ResolvePreference(null,true,true),"migration preserves registered autostart");
  string key=@"Software\ScreenWisp\Tests\"+Guid.NewGuid().ToString("N");
  try{
   var startup=new StartupRegistration(key,@"C:\ScreenWisp Stable\ScreenWisp.exe");startup.Set(false);check(!startup.Enabled,"disabled startup creates no registration");
   startup.Set(true);startup.Set(true);using(var registry=Registry.CurrentUser.OpenSubKey(key))check(registry!.GetValueNames().Length==1,"repeated autostart setup has one value");
   check(startup.RegisteredCommand==((char)34+@"C:\ScreenWisp Stable\ScreenWisp.exe"+(char)34+" --startup"),"autostart uses quoted absolute silent command");
   var moved=new StartupRegistration(key,@"C:\Moved ScreenWisp\ScreenWisp.exe");moved.Set(true);check(moved.RegisteredCommand==moved.Command,"manual launch repairs moved executable path");moved.Set(false);moved.Set(false);check(!moved.Enabled,"disabling removes registration idempotently");
  }finally{Registry.CurrentUser.DeleteSubKeyTree(key,false);}
  var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ScreenWisp.Ux-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{
   var store=new SettingsStore(System.IO.Path.Combine(dir,"settings.json"));var settings=new Settings{AutoStart=false};
   using var manager=new OverlayManager();manager.Reconcile();
   bool opened=false,quit=false;
   using var menu=new TrayMenuController(()=>opened=true,()=>quit=true);
   check(menu.Menu.Items.Cast<Forms.ToolStripItem>().Select(i=>i.Text).SequenceEqual(new[]{ScreenWisp.Localization.Localizer.Current["Settings"],ScreenWisp.Localization.Localizer.Current["Exit"]}),"right-click menu has only settings and exit");
   menu.Menu.Items[0].PerformClick();check(opened,"right-click settings command works");menu.Menu.Items[1].PerformClick();check(quit,"right-click exit command works");
   var settingsWindow=new SettingsWindow(settings,(_,_)=>{});check(settingsWindow.Icon is not null,"settings window explicitly uses branded icon");settingsWindow.Close();
   var popup=new BrushPopup(()=>settings,next=>{store.Save(next);settings=next;manager.Configure(next);},()=>{});
   try{
    var swatches=(StackPanel)popup.FindName("Swatches");
    foreach(Button button in swatches.Children){button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(store.Load().Color==(PenColor)button.Tag,"popup swatch applies and persists "+button.ToolTip);}
    var slider=(Slider)popup.FindName("WidthSlider");
    for(int i=0;i<PenOptions.Widths.Length;i++){slider.Value=i;check(store.Load().Width==PenOptions.Widths[i],"popup slider applies and persists "+PenOptions.Widths[i]);}
    check(((TextBlock)popup.FindName("WidthLabel")).Text=="12 px" && ((Ellipse)popup.FindName("Preview")).Fill==BrushPopup.ColorBrush(settings.Color),"popup preview follows selected brush");
   }finally{popup.Close();}
   check(store.Load().AutoStart==false,"quick settings preserve autostart preference");
   settings=settings with{TemporaryUndo=new(4,0x5A)};store.Save(settings);check(store.Load().TemporaryUndo==settings.TemporaryUndo,"session undo chord persists");
   File.WriteAllText(System.IO.Path.Combine(dir,"settings.json"),"{\"Width\":3}");check(store.Load().Width==4,"legacy pen width migrates without resetting preferences");
   manager.Configure(settings with{Color=PenColor.White,Width=12});manager.Toggle();
   foreach(var window in manager.Windows){Stroke(window);var canvas=(Canvas)window.FindName("StrokeCanvas");var line=(Polyline)canvas.Children[0];check(Math.Abs(line.StrokeThickness*VisualTreeHelper.GetDpi(window).DpiScaleX-12)<.01,"pen width is physical pixels on monitor");check(((SolidColorBrush)line.Stroke).Color==Colors.White,"new color reaches WPF renderer");}
   manager.Pause();manager.Clear();manager.Configure(new Settings());
   using var service=new TemporaryDrawingService(manager);var g=service.Gesture;long t=10000;
   void Begin(){g.Process(0xA4,true,t);g.Process(0xA4,false,t+30);g.Process(0xA4,true,t+100);t+=1000;}
   IntPtr Key(int vk,bool down,uint mods=1)=>service.ProcessPacket(new Win32.KbdllHookStruct{VkCode=vk},down,mods);
   var a=manager.Windows[0];var b=manager.Windows[^1];Begin();Stroke(a);Stroke(b);Stroke(a);
   check(Key(0x5A,true)!=IntPtr.Zero && manager.Windows.Sum(w=>w.TemporaryStrokeCount)==2 && manager.Temporary,"Z consumes chord and undoes global latest without ending Alt");
   Key(0x5A,true);check(manager.Windows.Sum(w=>w.TemporaryStrokeCount)==2,"Z autorepeat cannot undo more than one stroke");Key(0x5A,false);
   Key(0x5A,true);Key(0x5A,false);check(manager.Windows.Sum(w=>w.TemporaryStrokeCount)==1,"next distinct Z undoes preceding monitor stroke");
   MouseDown(a);Key(0x5A,true);Key(0x5A,false);check(a.IsMouseCaptured && manager.Windows.Sum(w=>w.TemporaryStrokeCount)==1,"undo removes only completed stroke and preserves active stroke; captured="+a.IsMouseCaptured+" count="+manager.Windows.Sum(w=>w.TemporaryStrokeCount)+" temporary="+manager.Temporary);
   Key(0x5A,true);Key(0x5A,false);check(manager.Temporary && a.IsMouseCaptured,"empty completed history is harmless");MouseUp(a);
   Key(0x5A,true);Key(0x5A,false);check(manager.Windows.All(w=>w.TemporaryStrokeCount==0),"formerly active stroke becomes undoable after mouse up");
   Stroke(a);Key(0xA4,false);check(!manager.Temporary&&manager.Windows.All(w=>w.TemporaryStrokeCount==0&&!w.IsMouseCaptured),"Alt release still clears all and releases capture after undo");
   check(manager.Windows.All(w=>(Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)!=0),"click-through restored on both monitors after undo session");
   check(Key(0x5A,true)==IntPtr.Zero && Key(0x5A,false)==IntPtr.Zero,"Z and Alt Z are never consumed outside temporary mode");
   manager.Configure(new Settings{TemporaryUndo=new(4,0x5A)});Begin();Stroke(b);Key(0xA0,true,1);check(manager.Temporary,"configured undo modifier does not cancel Alt");
   check(Key(0x5A,true,5)!=IntPtr.Zero&&b.TemporaryStrokeCount==0&&manager.Temporary,"Shift Z alternative undoes without ending Alt");Key(0x5A,false,5);Key(0xA0,false,5);
   check(Key(0x09,true)==IntPtr.Zero&&!manager.Temporary,"Alt Tab still passes and cancels temporary drawing");Key(0xA4,false);
   manager.Configure(new Settings());Key(0x77,true,6);Stroke(a);check(Key(0x5A,true,6)!=IntPtr.Zero&&a.TemporaryStrokeCount==0&&manager.Temporary,"Z also works during alternative hold");Key(0x5A,false,6);Key(0x77,false,6);
   check(!manager.Temporary,"alternative hold release still exits after undo");
   manager.Configure(new Settings{TemporaryUndo=new(4,0x5A)});Key(0x77,true,6);Stroke(b);
   check(Key(0x5A,true,6)!=IntPtr.Zero && b.TemporaryStrokeCount==0 && manager.Temporary,"configured undo modifier already held by alternative remains usable");Key(0x5A,false,6);Key(0x77,false,6);
   check(!manager.Temporary,"alternative cleanup remains safe with overlapping modifiers");
  }finally{Directory.Delete(dir,true);}
 }
 static void MouseDown(OverlayWindow w)=>w.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});
 static void MouseUp(OverlayWindow w)=>w.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});
 static void Stroke(OverlayWindow w){MouseDown(w);MouseUp(w);}
}
