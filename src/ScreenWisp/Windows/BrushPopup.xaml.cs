using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ScreenWisp.Core;
using ScreenWisp.Infrastructure;
using ScreenWisp.Localization;
namespace ScreenWisp.Windows;
internal partial class BrushPopup : Window {
 readonly Func<Settings> current;
 readonly Action<Settings> save;
 readonly Action openSettings;
 readonly DispatcherTimer dismiss;
 bool refreshing,mouseDown;
 HwndSource? source;
 IntPtr foreground;
 internal BrushPopup(Func<Settings> current,Action<Settings> save,Action openSettings){
  this.current=current;this.save=save;this.openSettings=openSettings;InitializeComponent();
  foreach(var option in PenOptions.Colors){
   var button=new Button{Width=38,Height=36,Tag=option.Color,ToolTip=option.Label};
   System.Windows.Automation.AutomationProperties.SetName(button,option.Label);
   var circle=new Ellipse{Width=22,Height=22,Fill=ColorBrush(option.Color),Stroke=new SolidColorBrush(System.Windows.Media.Color.FromRgb(85,104,121)),StrokeThickness=1};
   button.Content=new Border{Child=circle,CornerRadius=new CornerRadius(15),BorderThickness=new Thickness(2),Padding=new Thickness(3)};
   button.Click+=(_,_)=>{save(current() with{Color=option.Color});Refresh();};Swatches.Children.Add(button);
  }
  SourceInitialized+=(_,_)=>{
   var hwnd=new WindowInteropHelper(this).Handle;source=HwndSource.FromHwnd(hwnd);source.AddHook(WndProc);
   var style=Win32.GetWindowLongPtr(hwnd,-20).ToInt64()|0x08000000|0x80;
   Win32.SetWindowLongPtr(hwnd,-20,new IntPtr(style));
  };
  dismiss=new DispatcherTimer(TimeSpan.FromMilliseconds(40),DispatcherPriority.Input,(_,_)=>DismissIfNeeded(),Dispatcher);dismiss.Stop();
  Localizer.Current.Changed+=Refresh;
  Closed+=(_,_)=>{Localizer.Current.Changed-=Refresh;dismiss.Stop();source?.RemoveHook(WndProc);};Refresh();
 }
 internal static Brush ColorBrush(PenColor color)=>color switch{PenColor.White=>Brushes.White,PenColor.Red=>Brushes.Red,PenColor.Blue=>Brushes.Blue,PenColor.Green=>Brushes.Lime,PenColor.Purple=>Brushes.MediumOrchid,PenColor.Yellow=>Brushes.Yellow,_=>Brushes.Red};
 internal void Refresh(){
  refreshing=true;try{
   var settings=current();WidthSlider.Value=Array.IndexOf(PenOptions.Widths,PenOptions.NormalizeWidth(settings.Width));
   WidthLabel.Text=Localizer.Current.Format("WidthValue",settings.Width);Preview.Fill=ColorBrush(settings.Color);
   double dpi=VisualTreeHelper.GetDpi(this).DpiScaleX;Preview.Width=Preview.Height=settings.Width/dpi;
   foreach(Button button in Swatches.Children){
    string label=Localizer.Current["Color"+(PenColor)button.Tag];button.ToolTip=label;System.Windows.Automation.AutomationProperties.SetName(button,label);
    ((Border)button.Content).BorderBrush=(PenColor)button.Tag==settings.Color?new SolidColorBrush(System.Windows.Media.Color.FromRgb(102,230,208)):Brushes.Transparent;
   }
  }finally{refreshing=false;}
 }
 internal void ShowAt(int x,int y){
  foreground=GetForegroundWindow();mouseDown=Down(1)||Down(2);Refresh();
  var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x,y));var work=screen.WorkingArea;
  var monitor=MonitorEnumerator.GetMonitors().FirstOrDefault(m=>x>=m.BoundsPx.Left&&x<m.BoundsPx.Right&&y>=m.BoundsPx.Top&&y<m.BoundsPx.Bottom);
  double dpi=(monitor?.DpiX??96)/96.0;
  var bounds=PopupPlacement.Calculate(x,y,new Win32.Rect{Left=work.Left,Top=work.Top,Right=work.Right,Bottom=work.Bottom},(int)Math.Ceiling(Width*dpi),(int)Math.Ceiling(Height*dpi));
  new WindowInteropHelper(this).EnsureHandle();
  void Place(){Win32.SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),bounds.Left,bounds.Top,bounds.Width,bounds.Height,0x10);}
  Place();Show();Place();Dispatcher.BeginInvoke(()=>{if(IsVisible){Place();Refresh();}});dismiss.Start();
 }
 internal void Dismiss(){dismiss.Stop();if(IsMouseCaptureWithin)Mouse.Capture(null);Hide();}
 void DismissIfNeeded(){
  if(!IsVisible){dismiss.Stop();return;}
  if(Down(0x12)||Down(0x1B)){Dismiss();return;}
  var active=GetForegroundWindow();if(active!=foreground && active!=new WindowInteropHelper(this).Handle){Dismiss();return;}
  bool pressed=Down(1)||Down(2);var point=System.Windows.Forms.Cursor.Position;
  if(pressed&&!mouseDown&&!IsMouseCaptureWithin&&Win32.GetWindowRect(new WindowInteropHelper(this).Handle,out var rect)&&(point.X<rect.Left||point.X>=rect.Right||point.Y<rect.Top||point.Y>=rect.Bottom))Dismiss();
  mouseDown=pressed;
 }
 void WidthChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(refreshing||current is null)return;save(current() with{Width=PenOptions.Widths[(int)Math.Round(e.NewValue)]});Refresh();}
 void CloseClick(object sender,RoutedEventArgs e)=>Dismiss();
 void SettingsClick(object sender,RoutedEventArgs e){Dismiss();openSettings();}
 IntPtr WndProc(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x21){handled=true;return new IntPtr(3);}return IntPtr.Zero;}
 static bool Down(int key)=>(GetAsyncKeyState(key)&0x8000)!=0;
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
}
