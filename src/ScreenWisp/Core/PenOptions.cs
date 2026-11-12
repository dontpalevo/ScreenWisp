using System;
using System.Linq;
namespace ScreenWisp.Core;
internal static class PenOptions {
 internal static (PenColor Color,string Label)[] Colors => new[]{PenColor.White,PenColor.Red,PenColor.Blue,PenColor.Green,PenColor.Purple,PenColor.Yellow}.Select(color=>(color,ScreenWisp.Localization.Localizer.Current["Color"+color])).ToArray();
 internal static readonly double[] Widths={2,4,6,8,12};
 internal static double NormalizeWidth(double width)=>Widths.OrderBy(w=>Math.Abs(w-width)).ThenByDescending(w=>w).First();
}
