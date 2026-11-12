using System;
namespace ScreenWisp.Infrastructure;
internal static class PopupPlacement {
 internal static Win32.Rect Calculate(int x,int y,Win32.Rect work,int width,int height){
  width=Math.Min(width,Math.Max(1,work.Width-16));height=Math.Min(height,Math.Max(1,work.Height-16));
  int left=Math.Clamp(x-width+20,work.Left+8,Math.Max(work.Left+8,work.Right-width-8));
  int top=y<work.Top+work.Height/2?y+12:y-height-12;
  top=Math.Clamp(top,work.Top+8,Math.Max(work.Top+8,work.Bottom-height-8));
  return new(){Left=left,Top=top,Right=left+width,Bottom=top+height};
 }
}
