using System;
using System.IO;
namespace ScreenWisp.Infrastructure;
internal static class AppLog {
 static readonly object Gate = new();
 internal static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenWisp", "logs", "app.log");
 static readonly System.Collections.Concurrent.ConcurrentQueue<string> GestureQueue=new();
 internal static void Gesture(string message){if(GestureQueue.Count<256)GestureQueue.Enqueue(message);}
 internal static void FlushGesture(){while(GestureQueue.TryDequeue(out var message))Error("GESTURE "+message);}
 internal static void Info(string message) { }
 internal static void Error(string message, Exception? ex=null) { try { lock(Gate) { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!); if(File.Exists(LogPath) && new FileInfo(LogPath).Length>1048576) File.Move(LogPath,LogPath+".old",true); File.AppendAllText(LogPath,$"{DateTimeOffset.Now:O} {message} {ex}\n"); } } catch { } }
}