using System;
using System.IO;
using Microsoft.Win32;
using ScreenWisp.Localization;
namespace ScreenWisp.Infrastructure;
// Injectable key keeps registry tests away from the actual Windows Run entry.
internal sealed class StartupRegistration(string keyPath,string executable) {
 const string ValueName="ScreenWisp";
 internal string Command => $"\"{Path.GetFullPath(executable)}\" --startup";
 internal bool Enabled {get{using var key=Registry.CurrentUser.OpenSubKey(keyPath);return key?.GetValue(ValueName) is string;}}
 internal string? RegisteredCommand {get{using var key=Registry.CurrentUser.OpenSubKey(keyPath);return key?.GetValue(ValueName) as string;}}
 internal void Set(bool enabled){
  if(enabled){using var key=Registry.CurrentUser.CreateSubKey(keyPath);if(!Equals(key.GetValue(ValueName),Command))key.SetValue(ValueName,Command,RegistryValueKind.String);}
  else {using var key=Registry.CurrentUser.OpenSubKey(keyPath,true);key?.DeleteValue(ValueName,false);}
 }
 internal static bool ResolvePreference(bool? saved,bool settingsExisted,bool registered)=>saved ?? (!settingsExisted || registered);
}
internal static class StartupService {
 const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
 internal static string ManagedRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenWisp","Autostart");
 internal static bool IsApplicationExecutable=>string.Equals(Path.GetFileName(Environment.ProcessPath),"ScreenWisp.exe",StringComparison.OrdinalIgnoreCase);
 internal static bool Enabled=>new StartupRegistration(Key,Environment.ProcessPath!).Enabled;
 internal static void Set(bool enabled){
  if(!IsApplicationExecutable)throw new LocalizedOperationException("ErrorStartupExecutable");
  // Register the user-selected location. Never stage/copy the executable or its dependencies.
  // For portable relocation: disable autostart before moving, then enable at the new path.
  new StartupRegistration(Key,Environment.ProcessPath!).Set(enabled);
 }
 internal static void RemoveRegistration(){using var key=Registry.CurrentUser.OpenSubKey(Key,true);key?.DeleteValue("ScreenWisp",false);}
 internal static void RemoveOwnedRegistration(string installDirectory){
  using var key=Registry.CurrentUser.OpenSubKey(Key,true);var command=key?.GetValue("ScreenWisp") as string;
  if(command is null)return;
  if(command.StartsWith("\""+Path.Combine(installDirectory,"ScreenWisp.exe")+"\"",StringComparison.OrdinalIgnoreCase)||command.StartsWith("\""+ManagedRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))key!.DeleteValue("ScreenWisp",false);
 }
 internal static Settings Initialize(Settings settings,SettingsStore store){
  if(!IsApplicationExecutable)return settings;
  bool enabled=StartupRegistration.ResolvePreference(settings.AutoStart,store.Exists,Enabled);
  Set(enabled);var next=settings with{AutoStart=enabled};if(settings.AutoStart!=enabled)store.Save(next);return next;
 }
}
