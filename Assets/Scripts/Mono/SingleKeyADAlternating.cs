using UnityEngine;
using UnityEngine.Events;

public class SingleKeyADAlternating : RhythmKeyControllerBase
{
    public UnityEvent onADKeyFailed;
    public UnityEvent onADKeySucceeded;
    public PauseManager pauseManager;

    void Reset()
    {
        keyConfig.primaryKey = KeyCode.A;
        keyConfig.secondaryKey = KeyCode.D;  // 確保D鍵也有設定
        keyConfigPrefix = "AD";
    }

    // 只用A鍵觸發判定
    protected override void HandlePlayerInput()
    {
        if (isGameEnded) return;
        if (Input.GetKeyDown(keyConfig.primaryKey))
            OnKeyPressed(keyConfig.primaryKey);
    }

    // 根據期望鍵高亮對應視覺
    protected override void StartNextBeat()
    {
        if (pausedByManager) return;

        expectedKey = (beatCounter % 2 == 0) ? KeyCode.A : KeyCode.D;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        // 根據expectedKey高亮對應的視覺元素
        SetKeyColor(expectedKey, highlightKeyColor);
        beatCounter++;
    }

    // 重寫OnKeyPressed，讓A鍵能匹配A或D的期望
    protected override void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            if (pressedKey == keyConfig.primaryKey)
                ShowFeedback(keyConfig.primaryKey, missKeyColor);
            return;
        }

        // A鍵總是觸發成功判定（無論期望是A還是D）
        if (pressedKey == keyConfig.primaryKey)
            OnBeatSuccess();
    }

    protected override void OnBeatFailed()
    {
        base.OnBeatFailed();
        onADKeyFailed?.Invoke();
    }
}