using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
namespace ScreenWisp.Core;
internal sealed class OverlayManager : IDisposable {
 readonly Dictionary<IntPtr,(MonitorEnumerator.MonitorInfo Info,OverlayWindow Window)> overlays=new();
 readonly UndoHistory history=new();
 readonly UndoHistory temporaryHistory=new();
 internal bool Temporary {get;private set;}
 readonly DispatcherTimer refresh;
 bool disposed;
 internal bool Drawing {get;private set;}
 internal Settings Settings {get;private set;}=new();
 internal event Action? Changed;
 internal IReadOnlyList<OverlayWindow> Windows => overlays.Values.Select(v=>v.Window).ToList();
 internal OverlayManager() { refresh=new DispatcherTimer(TimeSpan.FromMilliseconds(350),DispatcherPriority.Background,(_,_)=>{refresh!.Stop();Reconcile();},System.Windows.Application.Current.Dispatcher);refresh.Stop();SystemEvents.DisplaySettingsChanged+=DisplayChanged; }
 void DisplayChanged(object? sender,EventArgs e) { System.Windows.Application.Current.Dispatcher.BeginInvoke(()=>{if(disposed)return;refresh.Stop();refresh.Start();}); }
 internal void Reconcile() {
  if(disposed)return;
  var monitors=MonitorEnumerator.GetMonitors();
  if(monitors.Count==0)throw new InvalidOperationException("No monitors enumerated");
  foreach(var key in overlays.Keys.ToList()) {
   var old=overlays[key];
   if(monitors.Any(m=>m==old.Info))continue;
   old.Window.FinishStroke();temporaryHistory.RemoveOwner(old.Window);history.RemoveOwner(old.Window);old.Window.Close();overlays.Remove(key);
  }
  foreach(var monitor in monitors) {
   if(overlays.ContainsKey(monitor.HMonitor))continue;
   var w=new OverlayWindow(monitor.BoundsPx,monitor.DpiX,monitor.DpiY);
   w.StrokeAdded+=(owner,stroke)=>history.Add(owner,()=>owner.RemoveStroke(stroke));
   w.TemporaryStrokeAdded+=(owner,stroke)=>temporaryHistory.Add(owner,()=>owner.RemoveTemporaryStroke(stroke));
   overlays.Add(monitor.HMonitor,(monitor,w));w.Show();
  }
  Apply();
 }
 internal void Configure(Settings settings) {Settings=settings;Apply();}
 void Apply(){foreach(var v in overlays.Values){v.Window.SetTemporary(Temporary);v.Window.SetPenColor(Settings.Color);v.Window.PenWidth=Settings.Width;v.Window.SetMode(Drawing || Temporary?OverlayMode.Draw:OverlayMode.PassThrough,Temporary);}}
 internal void BeginTemporary(){
  if(Temporary)return;
  Drawing=false; // permanent mode is paused; release MUST return to click-through
  foreach(var w in Windows){w.FinishStroke();w.ClearTemporary();}
  temporaryHistory.Clear();Temporary=true;Apply();Changed?.Invoke();
 }
 internal void EndTemporary(){
  if(!Temporary)return;
  // Disable input and release capture before clearing/restoring visual layers.
  foreach(var w in Windows)w.SetMode(OverlayMode.PassThrough);
  Temporary=false;Drawing=false;
  foreach(var w in Windows){w.ClearTemporary();w.SetTemporary(false);}
  temporaryHistory.Clear();Apply();Changed?.Invoke();
 }
 internal void Toggle(){if(Temporary)return;Drawing=!Drawing;Apply();Changed?.Invoke();}
 internal void Pause(){EndTemporary();Drawing=false;Apply();Changed?.Invoke();}
 internal void UndoTemporaryCompleted(){if(Temporary)temporaryHistory.Undo();}
 internal void Undo(){if(Temporary){UndoTemporaryCompleted();return;}foreach(var w in Windows)w.FinishStroke();history.Undo();}
 internal void Clear(){if(Temporary){foreach(var w in Windows)w.ClearTemporary();temporaryHistory.Clear();}else{foreach(var w in Windows)w.ClearAll();history.Clear();}}
 public void Dispose(){if(disposed)return;Pause();disposed=true;refresh.Stop();SystemEvents.DisplaySettingsChanged-=DisplayChanged;foreach(var w in Windows)w.Close();overlays.Clear();history.Clear();temporaryHistory.Clear();}
}