using UnityEngine;
using UnityEngine.Events;

public class JLController : RhythmKeyControllerBase
{
    public UnityEvent onJLKeyFailed;
    public PauseManager pauseManager;
    void Reset()
    {
        keyConfig.primaryKey = KeyCode.J;
        keyConfig.secondaryKey = KeyCode.L;
        keyConfigPrefix = "JL";
    }
    
    protected override void OnBeatFailed()
    {
        pauseManager.scriptsToPause.Remove(this);
        onJLKeyFailed.Invoke();
    }
}
