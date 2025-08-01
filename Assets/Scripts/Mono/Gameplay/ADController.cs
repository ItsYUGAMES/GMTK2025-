using UnityEngine;
using UnityEngine.Events;

public class ADController : RhythmKeyControllerBase
{
    public UnityEvent onADKeyFailed;
    public PauseManager pauseManager;
    void Reset()
    {
        keyConfig.primaryKey = KeyCode.A;
        keyConfig.secondaryKey = KeyCode.D;
        keyConfigPrefix = "AD";
    }

    protected override void OnBeatFailed()
    {
        pauseManager.scriptsToPause.Remove(this);
        onADKeyFailed.Invoke();
    }
}
