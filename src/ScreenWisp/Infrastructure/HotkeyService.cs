using System;
using System.Collections.Generic;
using System.ComponentModel;
using ScreenWisp.Localization;
using System.Windows.Interop;
namespace ScreenWisp.Infrastructure;
internal sealed class HotkeyService : IDisposable {
 readonly HwndSource source;
 readonly List<int> ids=new();
 int generation;
 readonly bool emergencyRegistered;
 internal event Action<int>? Pressed;
 internal HotkeyService() { source=new HwndSource(new HwndSourceParameters("ScreenWisp.Hotkeys") { Width=0,Height=0,WindowStyle=0 });source.AddHook(Hook);emergencyRegistered=Win32.RegisterHotKey(source.Handle,1,6|0x4000,0x7B);if(!emergencyRegistered)AppLog.Error("Emergency RegisterHotKey is unavailable; keyboard hook fallback remains active"); }
 internal void Apply(Settings settings) {
  settings.Validate();var next=new List<int>();var keys=new[]{settings.Toggle,settings.Clear,settings.Undo};
  // Keep unchanged bindings to permit transactional edits with the old set still live.
  var retained=new List<int>();
  try { for(int i=0;i<keys.Length;i++) {
   int existing = currentKeys is null ? -1 : Array.IndexOf(currentKeys, keys[i]);
   if(existing>=0) { next.Add(ids[existing]);retained.Add(ids[existing]);continue; }
   int id=100+(++generation)*4+i;
   if(!Win32.RegisterHotKey(source.Handle,id,keys[i].Modifiers|0x4000,keys[i].Key)) throw new LocalizedWin32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(),"ErrorHotkeyBusy",keys[i]);
   next.Add(id);
  }} catch { foreach(int id in next)if(!retained.Contains(id))Win32.UnregisterHotKey(source.Handle,id);throw; }
  foreach(int id in ids)if(!retained.Contains(id))Win32.UnregisterHotKey(source.Handle,id);
  ids.Clear();ids.AddRange(next);currentKeys=keys;
 }
 Hotkey[]? currentKeys;
 IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled) { if(msg==Win32.WmHotkey && w.ToInt32()==1 && emergencyRegistered){handled=true;Pressed?.Invoke(3);return IntPtr.Zero;} if(msg==Win32.WmHotkey) { int action=ids.IndexOf(w.ToInt32());if(action>=0){handled=true;Pressed?.Invoke(action);} }return IntPtr.Zero; }
 public void Dispose(){if(emergencyRegistered)Win32.UnregisterHotKey(source.Handle,1);foreach(int id in ids)Win32.UnregisterHotKey(source.Handle,id);ids.Clear();source.RemoveHook(Hook);source.Dispose();}
}
