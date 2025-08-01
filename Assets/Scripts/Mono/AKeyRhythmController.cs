using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 單鍵節奏控制器，只使用一個按鍵進行節奏操作
/// </summary>
public class SingleKeyController : RhythmKeyControllerBase
{
    [Header("單鍵設置")]
    public KeyCode singleKey = KeyCode.A;

    [Header("事件")]
    public UnityEvent onSingleKeyFailed;
    public UnityEvent onSingleKeySuccess;

    [Header("管理器引用")]
    public PauseManager pauseManager;

    void Reset()
    {
        // 設置為同一個按鍵，這樣節拍器只會使用一個鍵
        keyConfig.primaryKey = KeyCode.Space;
        keyConfig.secondaryKey = KeyCode.Space;
        keyConfigPrefix = "SingleKey";

        // 調整單鍵模式的參數
        beatInterval = 1.0f;
        successWindow = 0.4f;
        successToPass = 15;  // 單鍵可能需要更多成功次數
        failToLose = 3;      // 失敗容忍度可以降低
        needConsecutiveSuccessToResume = 3;
    }

    protected override void Awake()
    {
        // 確保兩個鍵都設置為同一個鍵
        keyConfig.primaryKey = singleKey;
        keyConfig.secondaryKey = singleKey;

        base.Awake();
    }

    protected override void Start()
    {
        // 只需要一個鍵的視覺反饋，隱藏第二個鍵
        if (secondaryKeySpriteRenderer != null)
        {
            secondaryKeySpriteRenderer.gameObject.SetActive(false);
        }

        base.Start();
    }

    protected override void HandlePlayerInput()
    {
        if (isGameEnded) return;

        // 只監聽單一按鍵
        if (Input.GetKeyDown(singleKey))
            OnKeyPressed(singleKey);
    }

    protected override void StartNextBeat()
    {
        if (pausedByManager) return;

        Debug.Log($"[{keyConfigPrefix}] 下一個節拍 - 按 {singleKey} 鍵");

        // 單鍵模式下，每個節拍都是同一個鍵
        expectedKey = singleKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        // 只高亮主鍵
        SetKeyColor(singleKey, highlightKeyColor);
        beatCounter++;
    }

    protected override void OnBeatSuccess()
    {
        base.OnBeatSuccess();
        onSingleKeySuccess?.Invoke();

        // 檢查是否達到通關條件
        if (successCount >= successToPass)
        {
            isGameEnded = true;
            OnGameSuccess();
        }
    }

    protected override void OnBeatFailed()
    {
        // 如果有 PauseManager，從暫停列表中移除
        if (pauseManager != null)
        {
            pauseManager.scriptsToPause.Remove(this);
        }

        base.OnBeatFailed();
        onSingleKeyFailed?.Invoke();
    }

    protected override void EnterPauseForFailure()
    {
        base.EnterPauseForFailure();
        Debug.Log($"[{keyConfigPrefix}] 單鍵失敗暫停，連續按 {singleKey} 鍵 {needConsecutiveSuccessToResume} 次恢復");
    }

    protected override void OnGameSuccess()
    {
        base.OnGameSuccess();
        Debug.Log($"[{keyConfigPrefix}] 單鍵挑戰通關！成功次數: {successCount}");
    }

    protected override void OnGameFail()
    {
        base.OnGameFail();
        Debug.Log($"[{keyConfigPrefix}] 單鍵挑戰失敗！失敗次數: {failCount}");
    }

    // 提供方法來動態更改單鍵
    public void SetSingleKey(KeyCode newKey)
    {
        singleKey = newKey;
        keyConfig.primaryKey = newKey;
        keyConfig.secondaryKey = newKey;
        expectedKey = newKey;
    }

    // 重置單鍵設置
    public override void StartRhythm()
    {
        // 確保鍵位設置正確
        keyConfig.primaryKey = singleKey;
        keyConfig.secondaryKey = singleKey;

        base.StartRhythm();
    }
}