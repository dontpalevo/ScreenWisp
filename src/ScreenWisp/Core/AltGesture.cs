using System;
namespace ScreenWisp.Core;
// Pure monotonic state machine: autorepeat and Alt chords cannot arm a session.
internal sealed class AltGesture(int doubleTapMs) {
 int downKey, armedKey, suppressedKey;
 long downAt, releasedAt, startedAt;
 bool cleanTap;
 internal bool Active {get;private set;}
 internal int HeldKey => downKey;
 internal event Action? Started;
 internal event Action? Ended;
 internal bool Process(int key,bool down,long now,bool blocked=false) {
  bool alt=key is 0xA4 or 0xA5;
  if(!alt) {
   if(down && key==0x1B && Active){Cancel();return true;}
   if(down){armedKey=0;cleanTap=false;if(Active && key is not (0x10 or 0xA0 or 0xA1))Cancel();}
   if(down && key==0x1B && Active){Cancel();return true;}
   return false;
  }
  if(!down) {
   bool swallow=suppressedKey==key;if(swallow)suppressedKey=0;
   if(downKey!=key)return swallow;
   if(Active){Cancel();return true;}
   if(cleanTap && now-downAt<=doubleTapMs){armedKey=key;releasedAt=now;}else armedKey=0;
   downKey=0;cleanTap=false;return swallow;
  }
  if(downKey==key)return Active || suppressedKey==key; // key repeat
  if(downKey!=0){Cancel();return false;} // both Alt keys: fail closed
  suppressedKey=0;
  downKey=key;downAt=now;cleanTap=!blocked;
  if(!blocked && armedKey==key && now-releasedAt<=doubleTapMs) {
   armedKey=0;cleanTap=false;Active=true;startedAt=now;suppressedKey=key;
   Started?.Invoke();return true;
  }
  armedKey=0;return false;
 }
 internal void Poll(bool held,bool desktopAvailable,long now,bool dispatcherStalled=false) {
  if(Active && (!held || !desktopAvailable || dispatcherStalled || now-startedAt>=120000))Cancel();
  else if(!Active && downKey!=0 && !held){downKey=0;armedKey=0;cleanTap=false;}
 }
 internal void Cancel(){bool wasActive=Active;Active=false;downKey=0;armedKey=0;cleanTap=false;if(wasActive)Ended?.Invoke();}
}
