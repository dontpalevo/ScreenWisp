using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using ScreenWisp.Core;
using ScreenWisp.Localization;
namespace ScreenWisp.Infrastructure;
public sealed record Hotkey(uint Modifiers, uint Key) {
 public override string ToString() => $"{((Modifiers&2)!=0?"Ctrl+":"")}{((Modifiers&1)!=0?"Alt+":"")}{((Modifiers&4)!=0?"Shift+":"")}{((Modifiers&8)!=0?"Win+":"")}{KeyInterop.KeyFromVirtualKey((int)Key)}";
 public static Hotkey Parse(string text, bool allowUnmodified=false) {
  uint mods=0; Key key=System.Windows.Input.Key.None;
  foreach(var part in text.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)) {
   switch(part.ToUpperInvariant()) { case "CTRL": mods|=2;break; case "ALT":mods|=1;break;case "SHIFT":mods|=4;break;case "WIN":mods|=8;break;
   default: if(key!=System.Windows.Input.Key.None || !Enum.TryParse(part,true,out key) || !((key>=System.Windows.Input.Key.A&&key<=System.Windows.Input.Key.Z)||(key>=System.Windows.Input.Key.F1&&key<=System.Windows.Input.Key.F24)||(key>=System.Windows.Input.Key.D0&&key<=System.Windows.Input.Key.D9))) throw new LocalizedArgumentException("ErrorHotkeySyntax"); break;
  }}
  if((mods==0 && !allowUnmodified) || key==System.Windows.Input.Key.None)throw new LocalizedArgumentException("ErrorHotkeyMissing");
  return new(mods,(uint)KeyInterop.VirtualKeyFromKey(key));
 }
}
public sealed record Settings {
 public Hotkey Toggle {get;init;} = new(6,0x44);
 public Hotkey Clear {get;init;} = new(6,0x43);
 public Hotkey Undo {get;init;} = new(6,0x5A);
 public Hotkey Hold {get;init;} = new(6,0x77); // Ctrl+Shift+F8
 public Hotkey TemporaryUndo {get;init;} = new(0,0x5A);
 public string Language {get;init;} = "auto";
 public bool? AutoStart {get;init;}
 public PenColor Color {get;init;} = PenColor.Red;
 public double Width {get;init;} = 4;
 public void Validate() {
  var keys=new[]{Toggle,Clear,Undo,Hold};
  if(keys.Any(k=>k is null))throw new LocalizedArgumentException("ErrorHotkeysMissing");
  foreach(var k in keys)if(k.Modifiers==0 || k.Modifiers>15 || Hotkey.Parse(k.ToString())!=k)throw new LocalizedArgumentException("ErrorHotkeyInvalid");
  if(keys.Distinct().Count()!=4)throw new LocalizedArgumentException("ErrorHotkeyDuplicate");
  if((Hold.Modifiers & 9)!=0)throw new LocalizedArgumentException("ErrorHoldModifiers");
  if(keys.Any(k=>k==new Hotkey(6,0x7B)))throw new LocalizedArgumentException("ErrorEmergencyReserved");
  if(TemporaryUndo is null || (TemporaryUndo.Modifiers & ~6u)!=0 || Hotkey.Parse(TemporaryUndo.ToString(),true)!=TemporaryUndo || TemporaryUndo.Key==Hold.Key)
   throw new LocalizedArgumentException("ErrorTemporaryUndo");
  if(!Enum.IsDefined(Color)|| !double.IsFinite(Width)||Width<1||Width>20)throw new LocalizedArgumentException("ErrorPenInvalid");
 }
}
internal sealed class SettingsStore(string path) {
 internal static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ScreenWisp","settings.json");
 internal bool Exists => File.Exists(path);
 internal Settings Load() { if(!File.Exists(path))return new(); try { var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty settings");s=s with{Language=Localizer.Normalize(s.Language)};s.Validate();return s with {Width=PenOptions.NormalizeWidth(s.Width)}; } catch(Exception ex) { if(path==DefaultPath)AppLog.Error("Settings load: defaults used",ex); try { File.Copy(path,path+".invalid",true); }catch{} return new(); } }
 internal void Save(Settings settings) { settings.Validate();Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true); }
}
