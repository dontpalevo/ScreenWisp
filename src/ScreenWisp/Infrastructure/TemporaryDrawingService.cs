using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenWisp.Core;
namespace ScreenWisp.Infrastructure;
internal sealed class TemporaryDrawingService : IDisposable {
 readonly OverlayManager manager;
 readonly TemporaryUndoKey undoKey=new();
 internal AltGesture Gesture {get;}
 readonly Win32.LowLevelKeyboardProc callback;
 readonly DispatcherTimer watchdog;
 readonly Dispatcher dispatcher;
 IntPtr hook;
 long lastPoll,holdStarted;
 bool disposed,holdActive;
 internal bool Enabled {get;set;}=true;
 internal event Action? EmergencyExitRequested;
 internal TemporaryDrawingService(OverlayManager manager) {
  this.manager=manager;dispatcher=System.Windows.Application.Current.Dispatcher;
  Gesture=new AltGesture(400);
  Gesture.Started+=()=>{AppLog.Gesture("Alt session BEGIN");manager.BeginTemporary();};
  Gesture.Ended+=()=>{AppLog.Gesture("Alt session END; clear and pass-through");manager.EndTemporary();};
  callback=Hook;
  hook=Win32.SetWindowsHookEx(Win32.WhKeyboardLl,callback,Win32.GetModuleHandle(null),0);
  if(hook==IntPtr.Zero){int error=Marshal.GetLastWin32Error();AppLog.Error($"Alt hook registration failed win32={error}");throw new Win32Exception(error);}
  AppLog.Gesture("hook installed WH_KEYBOARD_LL; Alt forwarded; double tap window=400ms");
  lastPoll=Environment.TickCount64;
  watchdog=new DispatcherTimer(TimeSpan.FromMilliseconds(40),DispatcherPriority.Send,(_,_)=>Poll(),dispatcher);
  SystemEvents.SessionSwitch+=SessionChanged;SystemEvents.PowerModeChanged+=PowerChanged;
 }
 internal void Cancel(){Gesture.Cancel();holdActive=false;manager.Pause();}
 static bool Down(int key)=>(GetAsyncKeyState(key)&0x8000)!=0;
 static uint Modifiers()=>(Down(0x11)?2u:0)|(Down(0x10)?4u:0)|(Down(0x12)?1u:0)|((Down(0x5B)||Down(0x5C))?8u:0);
 bool HoldDown(){var h=manager.Settings.Hold;return Down((int)h.Key) && (Modifiers()&h.Modifiers)==h.Modifiers;}
 void Poll() {
  if(disposed)return;
  long now=Environment.TickCount64;bool active=Gesture.Active||holdActive;
  bool held=Gesture.HeldKey!=0 && Down(Gesture.HeldKey),desktop=true;
  if(active){var d=OpenInputDesktop(0,false,0x0100);desktop=d!=IntPtr.Zero;if(d!=IntPtr.Zero){desktop=SwitchDesktop(d);CloseDesktop(d);}}
  if(active && (!desktop || now-lastPoll>1000)){AppLog.Gesture("safety cancel: desktop unavailable or dispatcher stalled");Cancel();}
  if(Gesture.Active && !held)AppLog.Gesture("watchdog: Alt no longer held");
  Gesture.Poll(held,desktop,now,now-lastPoll>1000);
  if(holdActive && (!HoldDown() || now-holdStarted>=120000)){AppLog.Gesture("hold safety/release cancel");Cancel();}
  lastPoll=now;
  if(manager.Temporary && !Gesture.Active && !holdActive)manager.EndTemporary();
  if((manager.Drawing || manager.Temporary) && Down(0x1B)){AppLog.Gesture("Escape poll cancel");Cancel();}
  AppLog.FlushGesture();
 }
 IntPtr Hook(int code,IntPtr w,IntPtr l) {
  if(code<0 || disposed)return Win32.CallNextHookEx(hook,code,w,l);
  try {
   var data=Marshal.PtrToStructure<Win32.KbdllHookStruct>(l);
   int msg=w.ToInt32();bool down=msg is Win32.WmKeydown or Win32.WmSyskeydown;
   if(msg is not (Win32.WmKeydown or Win32.WmSyskeydown or Win32.WmKeyup or Win32.WmSyskeyup))return Win32.CallNextHookEx(hook,code,w,l);
   if(ProcessPacket(data,down,Modifiers())!=IntPtr.Zero)return new IntPtr(1);
  }catch(Exception ex){AppLog.Error("Alt hook callback failed; disabling drawing",ex);Cancel();}
  return Win32.CallNextHookEx(hook,code,w,l);
 }
 internal IntPtr ProcessPacket(Win32.KbdllHookStruct data,bool down,uint modifiers){
   bool alt=data.VkCode is 0xA4 or 0xA5;
   if(alt)AppLog.Gesture($"Alt {(data.VkCode==0xA4?"L":"R")} {(down?"down":"up")} t={data.Time} injected={(data.Flags&0x10)!=0} active={Gesture.Active}");
   // Forward Alt INCLUDING second down/up. Suppression prevents async key state updating,
   // so the watchdog used to cancel every real session. Injected input follows the SAME path.
   if(down && data.VkCode==0x7B && (modifiers&6)==6){AppLog.Gesture("emergency exit requested");Cancel();EmergencyExitRequested?.Invoke();return new IntPtr(1);}
   if(down && data.VkCode==0x1B && (manager.Drawing||manager.Temporary)){AppLog.Gesture("Escape cancel");Cancel();return new IntPtr(1);}
   var undo=manager.Settings.TemporaryUndo;
   uint baseModifiers=Gesture.Active?1u:holdActive?manager.Settings.Hold.Modifiers:0;
   if(undoKey.Handle(data.VkCode,down,manager.Temporary && (Gesture.Active||holdActive),data.VkCode==undo.Key && modifiers==(baseModifiers|undo.Modifiers),manager.UndoTemporaryCompleted))return new IntPtr(1);
   // An extra modifier that belongs to the configured undo chord must not cancel the session.
   uint modifier=data.VkCode switch{0x11 or 0xA2 or 0xA3=>2u,0x10 or 0xA0 or 0xA1=>4u,_=>0};
   if(manager.Temporary && modifier!=0 && (undo.Modifiers & modifier)!=0 && (baseModifiers & modifier)==0)return IntPtr.Zero;
   var hold=manager.Settings.Hold;
   if(holdActive){
    bool released=!down && (data.VkCode==hold.Key || data.VkCode is 0x10 or 0xA0 or 0xA1 or 0x11 or 0xA2 or 0xA3);
    bool other=down && data.VkCode!=hold.Key && data.VkCode is not (0x10 or 0xA0 or 0xA1 or 0x11 or 0xA2 or 0xA3);
    if(released||other){AppLog.Gesture("hold END; release or other chord");Cancel();}
   }else if(Enabled && down && data.VkCode==hold.Key && modifiers==hold.Modifiers){
    Gesture.Cancel();holdActive=true;holdStarted=Environment.TickCount64;manager.BeginTemporary();AppLog.Gesture("alternative hold BEGIN");
   }
   bool blocked=!Enabled || (modifiers&11)!=0 || holdActive;
   bool wasActive=Gesture.Active;
   Gesture.Process(data.VkCode,down,Environment.TickCount64,blocked);
   if(wasActive && !Gesture.Active && !alt)AppLog.Gesture("Alt cancelled for other chord (key not recorded)");
  return IntPtr.Zero;
 }
 void SessionChanged(object sender,SessionSwitchEventArgs e)=>dispatcher.BeginInvoke(()=>{if(!disposed){AppLog.Gesture("session change cancel");Cancel();}});
 void PowerChanged(object sender,PowerModeChangedEventArgs e)=>dispatcher.BeginInvoke(()=>{if(!disposed){AppLog.Gesture("power change cancel");Cancel();}});
 public void Dispose(){if(disposed)return;disposed=true;watchdog.Stop();SystemEvents.SessionSwitch-=SessionChanged;SystemEvents.PowerModeChanged-=PowerChanged;if(hook!=IntPtr.Zero){if(!Win32.UnhookWindowsHookEx(hook))AppLog.Error($"hook removal failed win32={Marshal.GetLastWin32Error()}");hook=IntPtr.Zero;}Gesture.Cancel();holdActive=false;manager.Pause();AppLog.Gesture("hook disposed");AppLog.FlushGesture();}
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
 [DllImport("user32.dll")]static extern bool SwitchDesktop(IntPtr desktop);
 [DllImport("user32.dll")]static extern bool CloseDesktop(IntPtr desktop);
}
