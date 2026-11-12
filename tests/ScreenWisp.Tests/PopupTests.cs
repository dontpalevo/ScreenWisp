using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenWisp.Infrastructure;
using ScreenWisp.Windows;
using ScreenWisp.Core;
internal static class PopupTests {
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
 internal static void Run(Action<bool,string> check,string? output=null){
  var resource=Application.GetResourceStream(new Uri("pack://application:,,,/ScreenWisp;component/Assets/screenwisp.ico"))!;
  byte[] bytes;using(var memory=new MemoryStream()){using(resource.Stream)resource.Stream.CopyTo(memory);bytes=memory.ToArray();}
  using var reader=new BinaryReader(new MemoryStream(bytes));check(reader.ReadUInt16()==0&&reader.ReadUInt16()==1,"ICO directory signature");
  int count=reader.ReadUInt16();check(count==8,"ICO contains eight requested resolutions");
  var frames=new System.Collections.Generic.List<BitmapSource>();
  foreach(int expected in new[]{16,20,24,32,48,64,128,256}){
   int width=reader.ReadByte(),height=reader.ReadByte();if(width==0)width=256;if(height==0)height=256;reader.ReadByte();reader.ReadByte();reader.ReadUInt16();int bits=reader.ReadUInt16();int length=reader.ReadInt32(),offset=reader.ReadInt32();
   using var png=new MemoryStream(bytes,offset,length);var decoded=new PngBitmapDecoder(png,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
   var bitmap=new FormatConvertedBitmap(decoded,PixelFormats.Bgra32,null,0);var pixels=new byte[width*height*4];bitmap.CopyPixels(pixels,width*4,0);
   check(width==expected&&height==expected&&bits==32,"32-bit alpha icon frame "+expected);
   check(pixels[3]==0&&pixels[(width-1)*4+3]==0&&pixels[(width*(height-1))*4+3]==0&&pixels[^1]==0,"transparent icon corners "+expected);
   check(Enumerable.Range(0,width*height).Any(i=>pixels[i*4+3]==255)&&Enumerable.Range(0,width*height).Any(i=>pixels[i*4+3]>0&&pixels[i*4+3]<255),"solid S stroke and antialiased alpha edges "+expected);
   using var ico=new System.Drawing.Icon(new MemoryStream(bytes),new System.Drawing.Size(expected,expected));using var gdi=ico.ToBitmap();check(gdi.GetPixel(0,0).A==0,"Windows icon loader preserves alpha "+expected);frames.Add(bitmap);
  }
  foreach(var work in new[]{new Win32.Rect{Left=0,Top=0,Right=1920,Bottom=1040},new Win32.Rect{Left=-2560,Top=-1440,Right=0,Bottom=0}})
   foreach(var anchor in new[]{(work.Left,work.Top),(work.Right,work.Bottom),(work.Left,work.Bottom),(work.Right,work.Top)}){
    var bounds=PopupPlacement.Calculate(anchor.Item1,anchor.Item2,work,608,480);
    check(bounds.Left>=work.Left&&bounds.Right<=work.Right&&bounds.Top>=work.Top&&bounds.Bottom<=work.Bottom,"popup clamps to work area at taskbar edge / negative coordinates");
   }
  var settings=new Settings{Color=PenColor.White,Width=8};var popup=new BrushPopup(()=>settings,s=>settings=s,()=>{});
  try{
   var before=GetForegroundWindow();var cursor=System.Windows.Forms.Cursor.Position;popup.ShowAt(cursor.X,cursor.Y);popup.UpdateLayout();
   var hwnd=new WindowInteropHelper(popup).Handle;
   check(!popup.ShowActivated&&!popup.ShowInTaskbar&&(Win32.GetWindowLongPtr(hwnd,-20).ToInt64()&0x08000000)!=0,"popup is nonactivating tool window");
   check(GetForegroundWindow()==before,"opening popup preserves foreground app");
   check(SendMessage(hwnd,0x21,IntPtr.Zero,IntPtr.Zero).ToInt64()==3,"popup mouse activation returns MA_NOACTIVATE");
   check(!popup.IsMouseCaptured&&!popup.IsMouseCaptureWithin,"popup opening does not capture mouse");
   if(output is not null){Directory.CreateDirectory(output);var image=new RenderTargetBitmap((int)Math.Ceiling(popup.ActualWidth),(int)Math.Ceiling(popup.ActualHeight),96,96,PixelFormats.Pbgra32);image.Render(popup);Save(image,Path.Combine(output,"popup-preview.png"));RenderIcons(frames,Path.Combine(output,"icon-light-dark.png"));}
   popup.Dismiss();check(!popup.IsVisible&&!popup.IsMouseCaptured,"popup dismissal hides without mouse capture");
  }finally{popup.Close();}
 }
 static void Save(BitmapSource bitmap,string path){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);}
 static void RenderIcons(System.Collections.Generic.List<BitmapSource> frames,string path){var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){
  for(int row=0;row<2;row++){int top=row*330;dc.DrawRectangle(row==0?new SolidColorBrush(Color.FromRgb(243,243,243)):new SolidColorBrush(Color.FromRgb(32,32,32)),null,new Rect(0,top,920,330));
   var text=new FormattedText(row==0?"ScreenWisp — light surface":"ScreenWisp — dark surface",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),14,row==0?Brushes.Black:Brushes.White,1);dc.DrawText(text,new Point(14,top+8));
   double x=16;
   foreach(var frame in frames){dc.DrawImage(frame,new Rect(x,top+42,frame.PixelWidth,frame.PixelHeight));dc.DrawText(new FormattedText(frame.PixelWidth+" px",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,row==0?Brushes.Black:Brushes.White,1),new Point(x,top+frame.PixelHeight+52));x+=Math.Max(frame.PixelWidth,32)+26;}
  }}var target=new RenderTargetBitmap(920,660,96,96,PixelFormats.Pbgra32);target.Render(visual);Save(target,path);
 }
}
