using System;
using ScreenWisp.Localization;
using Forms=System.Windows.Forms;
namespace ScreenWisp.Infrastructure;
internal sealed class TrayMenuController : IDisposable {
 internal Forms.ContextMenuStrip Menu {get;}=new();
 internal TrayMenuController(Action openSettings,Action quit){
  Menu.Items.Add("",null,(_,_)=>openSettings());Menu.Items.Add("",null,(_,_)=>quit());
  Localizer.Current.Changed+=Translate;Translate();
 }
 void Translate(){Menu.Items[0].Text=Localizer.Current["Settings"];Menu.Items[1].Text=Localizer.Current["Exit"];}
 public void Dispose(){Localizer.Current.Changed-=Translate;Menu.Dispose();}
}
