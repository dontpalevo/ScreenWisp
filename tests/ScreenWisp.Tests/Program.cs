using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using Path=System.IO.Path;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
internal static class Program {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
 static int count;
 static void TestGesture(){
  var g=new AltGesture(400);int start=0,end=0;g.Started+=()=>start++;g.Ended+=()=>end++;
  g.Process(0xA4,true,0);g.Process(0xA4,true,30);Check(!g.Active,"single Alt and autorepeat stay inactive");
  g.Process(0xA4,false,40);Check(!g.Active,"single Alt release stays inactive");
  Check(g.Process(0xA4,true,150)&&g.Active,"state machine recognizes second Alt down");
  g.Process(0xA4,true,180);Check(start==1,"repeat cannot restart temporary session");
  Check(g.Process(0xA4,false,190)&&!g.Active&&end==1,"state machine Alt up ends exactly once");
  g.Process(0xA4,false,200);Check(end==1,"duplicate up is harmless");
  g.Process(0xA4,true,300);g.Process(0xA4,false,340);g.Process(0xA4,true,1000);Check(!g.Active,"slow double tap rejected");
  g.Cancel();g.Process(0xA4,true,1100);g.Process(0xA4,false,1800);g.Process(0xA4,true,1900);Check(!g.Active,"long first hold cannot arm");
  g.Cancel();g.Process(0xA4,true,2000);g.Process(0x41,true,2010);g.Process(0xA4,false,2040);g.Process(0xA4,true,2100);Check(!g.Active,"Alt chord cannot arm");
  g.Cancel();g.Process(0xA5,true,2200,true);g.Process(0xA5,false,2240);g.Process(0xA5,true,2300,true);Check(!g.Active,"Ctrl AltGr and disabled gestures rejected");
  g.Cancel();g.Process(0xA4,true,2400);g.Process(0xA4,false,2440);g.Process(0xA5,true,2500);Check(!g.Active,"different Alt keys cannot accidentally trigger");
  g.Cancel();g.Process(0xA5,true,2600);g.Process(0xA5,false,2640);g.Process(0xA5,true,2700);Check(g.Active,"right Alt double tap supported");
  g.Poll(true,true,122700);Check(!g.Active,"hard hold limit fails closed");
  g.Cancel();g.Process(0xA4,true,130000);g.Process(0xA4,false,130020);g.Process(0xA4,true,130421);Check(!g.Active,"401 ms is too late");
  g.Cancel();g.Process(0xA4,true,131000);g.Process(0xA4,false,131020);g.Process(0xA4,true,131420);Check(g.Active,"400 ms is accepted");
  g.Process(0x09,true,131430);Check(!g.Active,"Alt Tab cancels drawing without consuming Tab");

 }

 static void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
 static void Reject(Action action,string name){try{action();}catch(ArgumentException){Check(true,name);return;}throw new Exception(name);}
 [STAThread] static int Main(string[] args){try{
  if(args.Contains("--lifecycle")) {
   var app=new ScreenWisp.App{InstanceName="ScreenWisp.LifecycleTest-"+Guid.NewGuid().ToString("N")};app.InitializeComponent();
   bool exercised=false;
   var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
   timer.Tick+=(_,_)=>{
    timer.Stop();exercised=true;
    T Field<T>(string name)=>(T)typeof(ScreenWisp.App).GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(app)!;
    var popup=Field<BrushPopup>("brushPopup");var manager=Field<OverlayManager>("overlays");var temporary=Field<TemporaryDrawingService>("temporary");
    var point=System.Windows.Forms.Cursor.Position;popup.ShowAt(point.X,point.Y);Check(popup.IsVisible,"App left-click popup can open");
    var g=temporary.Gesture;long now=Environment.TickCount64;g.Process(0xA4,true,now);g.Process(0xA4,false,now+25);g.Process(0xA4,true,now+100);
    Check(manager.Temporary && !popup.IsVisible,"App hides popup synchronously when Alt session starts");
    g.Process(0xA4,false,now+150);Check(!manager.Temporary && manager.Windows.All(w=>(Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)!=0),"App Alt release returns all monitors to click-through after popup");
    app.Shutdown();
   };timer.Start();
   int result=app.Run();Check(exercised&&result==0,"full App startup and graceful shutdown (not duplicate-instance exit)");
   return 0;
  }

  new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  if(args.Contains("--screenshots")){ReleaseScreenshotSession.Run(args.Length>1?args[1]:"en-US");return 0;}
  if(args.Contains("--native-repro")){NativeInputProbe.Reproduce();return 0;}
  if(args.Contains("--native-input")){NativeInputProbe.Verify();return 0;}
  LocalizationTests.Run(Check,args.Contains("--localization-qa")?args[^1]:null);
  AboutTests.Run(Check);
  PopupTests.Run(Check,args.Contains("--visual-qa")?args[^1]:null);
  UxTests.Run(Check);
  TestGesture();
  var s=new Settings();s.Validate();Check(Hotkey.Parse("Ctrl+Shift+D")==s.Toggle,"hotkey parsing");
  Reject(()=>Hotkey.Parse("D"),"modifier required");Reject(()=>Hotkey.Parse("Ctrl+A+B"),"reject multiple keys");Reject(()=>(s with{Undo=s.Clear}).Validate(),"reject duplicates");Reject(()=>(s with{Width=double.NaN}).Validate(),"reject invalid width");
  var dir=Path.Combine(Path.GetTempPath(),"ScreenWisp.Tests-"+Guid.NewGuid());Directory.CreateDirectory(dir);
  try{var path=Path.Combine(dir,"settings.json");var store=new SettingsStore(path);Check(store.Load()==s,"missing settings defaults");store.Save(s with{Width=8});Check(store.Load().Width==8,"settings roundtrip");File.WriteAllText(path,"broken");Check(store.Load()==s && File.Exists(path+".invalid"),"corrupt settings recovery");}finally{Directory.Delete(dir,true);}
  Reject(()=>(s with{Hold=s.Toggle}).Validate(),"hold must differ from toggle");
  Reject(()=>(s with{Hold=new(1,0x77)}).Validate(),"hold cannot hijack Alt system chords");
  Reject(()=>(s with{Undo=new(6,0x7B)}).Validate(),"emergency exit remains reserved");
  var history=new UndoHistory();var owner=new object();int value=0;history.Add(owner,()=>value=1);history.Add(new object(),()=>value=2);history.Undo();Check(value==2,"global undo order");history.RemoveOwner(owner);history.Undo();Check(value==2&&history.Count==0,"disconnected monitor undo cleanup");
  using(var hotkeys=new HotkeyService()){
   var custom=s with{Toggle=new(7,0x77),Clear=new(7,0x78),Undo=new(7,0x79)};hotkeys.Apply(custom);hotkeys.Apply(custom);
   using var competitor=new HotkeyService();bool conflict=false;try{competitor.Apply(custom);}catch(System.ComponentModel.Win32Exception){conflict=true;}Check(conflict,"native hotkey conflict detection");
   var changed=custom with{Toggle=new(7,0x76)};hotkeys.Apply(changed);hotkeys.Apply(custom);hotkeys.Apply(custom with{Toggle=custom.Undo,Undo=custom.Toggle});hotkeys.Apply(custom);Check(true,"native hotkey rebind and unchanged retention");
  }
  using(var manager=new OverlayManager()){
   manager.Reconcile();Check(manager.Windows.Count==MonitorEnumerator.GetMonitors().Count,"one overlay per monitor");
   foreach(var w in manager.Windows){var hwnd=new WindowInteropHelper(w).Handle;Check((Win32.GetWindowLongPtr(hwnd,-20).ToInt64()&0x20)!=0,"pass-through native style");Check(!w.ShowInTaskbar&&!w.ShowActivated,"no taskbar or activation");Win32.GetWindowRect(hwnd,out var actual);Check(actual.Equals(w.MonitorBoundsPx),"physical pixel bounds");}
   manager.Toggle();foreach(var w in manager.Windows)Check((Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)==0,"drawing on every monitor");
   void Stroke(OverlayWindow w) {
    w.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});
    w.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});
   }
   var first=manager.Windows[0];Stroke(first);Check(first.StrokeCount==1,"WPF stroke input collected");
   var last=manager.Windows[^1];Stroke(last);manager.Undo();Check(manager.Windows.Sum(w=>w.StrokeCount)==1,"undo latest stroke across monitors");manager.Undo();Check(manager.Windows.All(w=>w.StrokeCount==0),"undo preceding stroke");
   Stroke(first);manager.Clear();manager.Undo();Check(first.StrokeCount==0,"clear resets undo history");
   // Use the actual service wiring (native hook installed), monotonic packets and real WPF windows.
   using(var temporary=new TemporaryDrawingService(manager)) {
    var g=temporary.Gesture;long t=10000;
    void Begin(){g.Process(0xA4,true,t);g.Process(0xA4,false,t+40);g.Process(0xA4,true,t+120);t+=1000;}
    manager.Clear();manager.Pause();
    Begin();Check(manager.Temporary && !manager.Drawing,"double Alt starts temporary mode");
    foreach(var w in manager.Windows)Stroke(w);
    Check(manager.Windows.All(w=>w.TemporaryStrokeCount==1 && w.StrokeCount==0),"temporary strokes on both monitors are isolated");
    g.Process(0xA4,false,t);Check(!manager.Temporary,"Alt release synchronously ends session");
    Check(manager.Windows.All(w=>w.TemporaryStrokeCount==0 && !w.IsMouseCaptured),"release clears every temporary stroke and capture");
    Check(manager.Windows.All(w=>(Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)!=0),"release restores click-through on both monitors");
    Check(manager.Windows.All(w=>SendMessage(new WindowInteropHelper(w).Handle,0x84,IntPtr.Zero,IntPtr.Zero).ToInt64()==-1),"native hit-test returns HTTRANSPARENT on both monitors");
    Begin();Check(manager.Windows.All(w=>w.TemporaryStrokeCount==0),"next Alt session starts clean");
    first.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});
    g.Process(0xA4,false,t);Check(first.TemporaryStrokeCount==0&&!first.IsMouseCaptured,"Alt released mid-stroke clears active stroke");
    manager.Toggle();Stroke(first);manager.Pause();
    Begin();Stroke(last);manager.Undo();Check(last.TemporaryStrokeCount==0&&first.StrokeCount==1,"temporary undo cannot touch permanent ink");
    Stroke(last);g.Process(0xA4,false,t);Check(first.StrokeCount==1&&!manager.Drawing,"permanent ink survives temporary release without input capture");
    manager.Undo();Check(first.StrokeCount==0,"permanent history is independent");
    Begin();Stroke(first);g.Poll(false,true,t);Check(!manager.Temporary && first.TemporaryStrokeCount==0,"lost Alt-up watchdog cleans ink");
    Begin();Stroke(last);g.Process(0x1B,true,t);Check(!manager.Temporary&&last.TemporaryStrokeCount==0,"Escape emergency clears session");
    g.Process(0xA4,false,t);Begin();Stroke(last);g.Poll(true,false,t);Check(!manager.Temporary&&last.TemporaryStrokeCount==0,"desktop loss clears session");
    g.Process(0xA4,false,t);Begin();Stroke(first);g.Poll(true,true,t,true);Check(!manager.Temporary&&first.TemporaryStrokeCount==0,"dispatcher stall fails closed");
    g.Process(0xA4,false,t);Begin();Stroke(last);temporary.Cancel();Check(!manager.Temporary&&!manager.Drawing&&last.TemporaryStrokeCount==0,"settings/session cancel fails closed");
    t=Environment.TickCount64;Begin();Stroke(first);
    var frame=new System.Windows.Threading.DispatcherFrame();int ticks=0;
    var probe=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(10)};
    probe.Tick+=(_,_)=>{if(!manager.Temporary || ++ticks>=100){probe.Stop();frame.Continue=false;}};
    probe.Start();System.Windows.Threading.Dispatcher.PushFrame(frame);
    Check(!manager.Temporary && first.TemporaryStrokeCount==0,"actual watchdog timer detects physically released Alt and clears UI");
   }
   var previous=manager.Windows.ToArray();manager.Reconcile();Check(previous.SequenceEqual(manager.Windows),"unchanged monitors retained");
   manager.Pause();manager.Clear();manager.Undo();Check(manager.Windows.All(w=>(Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)!=0),"pause restores click-through");
  }
  Console.WriteLine($"{count} checks passed");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
