using System;
namespace ScreenWisp.Core;
// Capture the matching down/up pair only inside a temporary session; repeats do not undo.
internal sealed class TemporaryUndoKey {
 int captured;
 internal bool Handle(int key,bool down,bool active,bool matches,Action undo){
  if(captured==key){if(!down)captured=0;return true;}
  if(down && active && matches){captured=key;undo();return true;}
  return false;
 }
}
