using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
using ScreenWisp.Localization;
using Forms=System.Windows.Forms;
namespace ScreenWisp;
public partial class App : Application {
 internal string InstanceName {get;init;}="ScreenWisp";
 EventWaitHandle? readySignal;
 EventWaitHandle? exitRequest;
 RegisteredWaitHandle? exitWait;
 EventWaitHandle? settingsRequest;
 RegisteredWaitHandle? settingsWait;
 Mutex? mutex;
 bool ownsMutex;
 OverlayManager? overlays;
 HotkeyService? hotkeys;
 TemporaryDrawingService? temporary;
 Forms.NotifyIcon? tray;
 TrayMenuController? trayMenu;
 BrushPopup? brushPopup;
 System.Drawing.Icon? icon;
 SettingsWindow? settingsWindow;
 bool hotkeyError;
 Settings settings=new();
 readonly SettingsStore store=new(SettingsStore.DefaultPath);
 protected override void OnStartup(StartupEventArgs e) {
  base.OnStartup(e);
  DispatcherUnhandledException+=(_,args)=>{AppLog.Error("Fatal UI error",args.Exception);args.Handled=true;Shutdown(1);};
  AppDomain.CurrentDomain.UnhandledException+=(_,args)=>AppLog.Error("Unhandled error",args.ExceptionObject as Exception);
  if(Array.Exists(e.Args,a=>a=="--shutdown")){
   try{using var request=EventWaitHandle.OpenExisting(@"Local\"+InstanceName+".Exit");request.Set();}catch(WaitHandleCannotBeOpenedException){}
   Shutdown();return;
  }
  if(Array.Exists(e.Args,a=>a=="--remove-startup")){
   try{StartupService.RemoveOwnedRegistration(AppContext.BaseDirectory);Shutdown();}catch(Exception ex){AppLog.Error("Uninstall startup cleanup failed",ex);Shutdown(1);}return;
  }
  if(Array.Exists(e.Args,a=>a=="--configure-startup")){
   try{StartupService.Initialize(store.Load(),store);Shutdown();}catch(Exception ex){AppLog.Error("Installer startup configuration failed",ex);Shutdown(1);}return;
  }
  mutex=new Mutex(true,@"Local\"+InstanceName+".SingleInstance",out ownsMutex);
  if(!ownsMutex){
   if(Array.Exists(e.Args,a=>a=="--settings"))try{using var request=EventWaitHandle.OpenExisting(@"Local\"+InstanceName+".Settings");request.Set();}catch(WaitHandleCannotBeOpenedException){}
   Shutdown();return;
  }
  readySignal=new EventWaitHandle(false,EventResetMode.ManualReset,@"Local\"+InstanceName+".Ready");
  exitRequest=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\"+InstanceName+".Exit");
  exitWait=ThreadPool.RegisterWaitForSingleObject(exitRequest,(_,_)=>Dispatcher.BeginInvoke(()=>Shutdown()),null,Timeout.Infinite,false);
  settingsRequest=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\"+InstanceName+".Settings");
  settingsWait=ThreadPool.RegisterWaitForSingleObject(settingsRequest,(_,_)=>Dispatcher.BeginInvoke(()=>{if(tray is not null)OpenSettings();}),null,Timeout.Infinite,false);
  try {
   settings=store.Load();Localizer.Current.Apply(settings.Language);
   try{settings=StartupService.Initialize(settings,store);}catch(Exception ex){AppLog.Error("Autostart initialization failed; tray remains available",ex);}
   overlays=new OverlayManager();overlays.Configure(settings);overlays.Reconcile();
   temporary=new TemporaryDrawingService(overlays);
   temporary.EmergencyExitRequested+=()=>Dispatcher.BeginInvoke(()=>Shutdown());
   hotkeys=new HotkeyService();hotkeys.Pressed+=action=>{switch(action){case 0:overlays.Toggle();break;case 1:overlays.Clear();break;case 2:overlays.Undo();break;case 3:Shutdown();break;}};
   var stream=GetResourceStream(new Uri("pack://application:,,,/ScreenWisp;component/Assets/screenwisp.ico"))!.Stream;
   using(stream)icon=new System.Drawing.Icon(stream,Forms.SystemInformation.SmallIconSize);
   trayMenu=new TrayMenuController(OpenSettings,()=>Shutdown());
   brushPopup=new BrushPopup(()=>settings,SavePen,OpenSettings);
   tray=new Forms.NotifyIcon{Icon=icon,Text=Localizer.Current["TrayIdle"],ContextMenuStrip=trayMenu.Menu,Visible=true};
   tray.MouseClick+=(_,args)=>{
    if(args.Button==Forms.MouseButtons.Left){if(brushPopup.IsVisible)brushPopup.Dismiss();else{var point=Forms.Cursor.Position;brushPopup.ShowAt(point.X,point.Y);}}
    else if(args.Button==Forms.MouseButtons.Right)brushPopup.Dismiss();
   };
   overlays.Changed+=()=>{if(overlays.Temporary||overlays.Drawing)brushPopup.Dismiss();UpdateTrayText();};
   try {hotkeys.Apply(settings);}catch(Exception ex){AppLog.Error("Hotkey registration failed; tray remains available",ex);hotkeyError=true;UpdateTrayText();}
   Localizer.Current.Changed+=UpdateTrayText;
   if(Array.Exists(e.Args,a=>a=="--settings"))OpenSettings();
   readySignal.Set();AppLog.Gesture("App startup ready 1.0.0-rc.3");
   // Silent startup: no palette, console, balloons, update requests or settings window.
  } catch(Exception ex){AppLog.Error("Startup failed",ex);Shutdown(1);}
 }
 void SavePen(Settings next){try{store.Save(next);settings=next;overlays!.Configure(next);}catch(Exception ex){AppLog.Error("Pen settings save failed",ex);brushPopup?.Dismiss();MessageBox.Show(Localizer.Current.Format("PenSaveError",Localizer.Error(ex)),"ScreenWisp",MessageBoxButton.OK,MessageBoxImage.Error);}}
 void OpenSettings(){brushPopup?.Dismiss();temporary!.Enabled=false;temporary.Cancel();overlays!.Pause();if(settingsWindow is not null){settingsWindow.Activate();return;}
  settingsWindow=new SettingsWindow(settings,(next,startup)=>{
   next=next with{AutoStart=startup};
   var previous=settings;var oldStartup=StartupService.Enabled;
   hotkeys!.Apply(next);
   try {StartupService.Set(startup);store.Save(next);} catch {hotkeys.Apply(previous);StartupService.Set(oldStartup);throw;}
   settings=next;hotkeyError=false;Localizer.Current.Apply(next.Language);overlays.Configure(next);brushPopup?.Refresh();
  },preference=>{var next=settings with{Language=preference};store.Save(next);settings=next;});settingsWindow.Closed+=(_,_)=>{settingsWindow=null;temporary.Enabled=true;};settingsWindow.Show();
 }
 void UpdateTrayText(){if(tray is not null)tray.Text=Localizer.Current[hotkeyError?"TrayHotkeys":overlays!.Temporary?"TrayTemporary":overlays.Drawing?"TrayDrawing":"TrayIdle"];}
 protected override void OnExit(ExitEventArgs e){Localizer.Current.Changed-=UpdateTrayText;readySignal?.Dispose();exitWait?.Unregister(null);exitRequest?.Dispose();settingsWait?.Unregister(null);settingsRequest?.Dispose();settingsWindow?.Close();brushPopup?.Close();temporary?.Dispose();hotkeys?.Dispose();overlays?.Dispose();if(tray is not null){tray.Visible=false;trayMenu?.Dispose();tray.Dispose();}icon?.Dispose();if(ownsMutex)mutex?.ReleaseMutex();mutex?.Dispose();base.OnExit(e);}
}
