using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ScreenWisp.Infrastructure;
using ScreenWisp.Core;
using System.Linq;
using System.Windows.Input;
internal static class NativeInputProbe {
 [StructLayout(LayoutKind.Explicit,Size=40)] internal struct Input {
  [FieldOffset(0)]public uint Type;
  [FieldOffset(8)]public ushort Key;
  [FieldOffset(12)]public uint Flags;
 }
 [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint count,Input[] input,int size);
 [DllImport("user32.dll")]internal static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
 static IntPtr target;
 static readonly System.Collections.Generic.HashSet<ushort> pressed=new();
 static void ReleaseTestKeys(){foreach(var key in pressed)SendInput(1,new[]{new Input{Type=1,Key=key,Flags=2}},40);pressed.Clear();}
 internal static void Key(ushort key,bool down){if(GetForegroundWindow()!=target)throw new Exception("Probe lost foreground; refusing keyboard input");if(SendInput(1,new[]{new Input{Type=1,Key=key,Flags=down?0u:2u}},40)!=1)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());if(down)pressed.Add(key);else pressed.Remove(key);Pump(25);}
 internal static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 internal static Window Open(){var w=new Window{Title="ScreenWisp — isolated keyboard test",Width=440,Height=160,Content="Automated Alt input targets only this test window."};w.Show();w.Activate();target=new WindowInteropHelper(w).Handle;SetForegroundWindow(target);Pump(80);if(GetForegroundWindow()!=target){Console.WriteLine("Waiting up to 45 seconds for focus on isolated test window");for(int retry=0;retry<90 && GetForegroundWindow()!=target;retry++)Pump(500); }if(GetForegroundWindow()!=target)throw new Exception($"Could not focus keyboard test window: expected={target} actual={GetForegroundWindow()}");return w;}
 internal static void Reproduce(){using var owner=new WindowScope(Open());bool suppress=true;int packets=0;
  Win32.LowLevelKeyboardProc proc=(code,w,l)=>{if(code>=0){var data=Marshal.PtrToStructure<Win32.KbdllHookStruct>(l);if(data.VkCode==0xA4){packets++;Console.WriteLine($"Alt packet msg={w.ToInt32():X} injected={(data.Flags&16)!=0} suppressed={suppress}");if(suppress)return new IntPtr(1);}}return Win32.CallNextHookEx(IntPtr.Zero,code,w,l);};
  var hook=Win32.SetWindowsHookEx(13,proc,Win32.GetModuleHandle(null),0);if(hook==IntPtr.Zero)throw new Exception("Probe hook failed");
  try {Key(0xA4,true);Pump(100);bool swallowedHeld=(GetAsyncKeyState(0xA4)&0x8000)!=0;Key(0xA4,false);suppress=false;Key(0xA4,true);Pump(100);bool forwardedHeld=(GetAsyncKeyState(0xA4)&0x8000)!=0;Key(0xA4,false);Console.WriteLine($"EVIDENCE suppressed Alt asyncDown={swallowedHeld}; forwarded Alt asyncDown={forwardedHeld}; hookPackets={packets}");if(swallowedHeld||!forwardedHeld||packets<4)throw new Exception("Suppression hypothesis not reproduced");}finally{Win32.UnhookWindowsHookEx(hook);ReleaseTestKeys();GC.KeepAlive(proc);}
 }
 internal static void Verify(){
  using var owner=new WindowScope(Open());
  using var manager=new OverlayManager();manager.Reconcile();
  using var service=new TemporaryDrawingService(manager);
  void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS native "+message);}
  void StrokeAll(){foreach(var window in manager.Windows){window.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});window.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});}}
  try {
   foreach(ushort alt in new ushort[]{0xA4,0xA5}){
    Key(alt,true);Key(alt,false);Pump(450);Assert(!manager.Temporary,$"single Alt {alt:X} stays inactive");
    Key(alt,true);Key(alt,false);Key(alt,true);Pump(220);
    Assert(manager.Temporary && (GetAsyncKeyState(alt)&0x8000)!=0,$"Alt {alt:X} survives five real watchdog ticks");
    StrokeAll();Assert(manager.Windows.All(w=>w.TemporaryStrokeCount==1),"WPF strokes exist on both screens");
    Key(alt,false);Assert(!manager.Temporary && manager.Windows.All(w=>w.TemporaryStrokeCount==0&&!w.IsMouseCaptured),"Alt up through Windows hook clears all screens and capture");
    Assert(manager.Windows.All(w=>(Win32.GetWindowLongPtr(new WindowInteropHelper(w).Handle,-20).ToInt64()&0x20)!=0),"click-through after native Alt up");
    Key(alt,true);Key(alt,false);Key(alt,true);Assert(manager.Temporary&&manager.Windows.All(w=>w.TemporaryStrokeCount==0),"next gesture starts clean");Key(alt,false);
   }
   Key(0xA2,true);Key(0xA0,true);Key(0x77,true);Pump(160);Assert(manager.Temporary,"Ctrl Shift F8 hold activates through Windows hook");StrokeAll();Key(0x77,false);Assert(!manager.Temporary&&manager.Windows.All(w=>w.TemporaryStrokeCount==0),"alternative release clears");Key(0xA0,false);Key(0xA2,false);
   manager.Configure(manager.Settings with{Hold=new(6,0x78)});
   Key(0xA2,true);Key(0xA0,true);Key(0x78,true);Assert(manager.Temporary,"reconfigured Ctrl Shift F9 hold activates");Key(0xA0,false);Assert(!manager.Temporary,"modifier release also ends hold");Key(0x78,false);Key(0xA2,false);
   Key(0xA4,true);Key(0xA4,false);Key(0xA4,true);StrokeAll();Key(0x1B,true);Assert(!manager.Temporary&&manager.Windows.All(w=>w.TemporaryStrokeCount==0),"native Escape emergency cleanup");Key(0x1B,false);Key(0xA4,false);
   bool exit=false;service.EmergencyExitRequested+=()=>exit=true;Key(0xA2,true);Key(0xA0,true);Key(0x7B,true);Assert(exit,"emergency exit request through native hook");Key(0x7B,false);Key(0xA0,false);Key(0xA2,false);
  } finally { // Always release only test keys, even if a probe assertion fails.
   ReleaseTestKeys();
   service.Cancel();
  }
 }
 sealed class WindowScope(Window w):IDisposable{public void Dispose()=>w.Close();}
}
