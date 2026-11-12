using System;
using System.Collections.Generic;
namespace ScreenWisp.Core;
internal sealed class UndoHistory {
 readonly List<(object Owner, Action Undo)> entries = new();
 internal int Count => entries.Count;
 internal void Add(object owner, Action undo) => entries.Add((owner,undo));
 internal void RemoveOwner(object owner) => entries.RemoveAll(e=>ReferenceEquals(e.Owner,owner));
 internal void Clear() => entries.Clear();
 internal void Undo() { if(entries.Count==0)return; var last=entries[^1]; entries.RemoveAt(entries.Count-1); last.Undo(); }
}