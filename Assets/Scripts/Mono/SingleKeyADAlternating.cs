using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class SingleKeyADAlternating : RhythmKeyControllerBase
{
    public UnityEvent onADKeyFailed;
    public UnityEvent onADKeySucceeded;
    public PauseManager pauseManager;
    public GameManager gameManager;
    void Reset()
    {
        keyConfig.primaryKey = KeyCode.A;
        keyConfig.secondaryKey = KeyCode.D;
        keyConfigPrefix = "AD";
    }

    // 只检测A键输入
    protected override void HandlePlayerInput()
    {
        if (isGameEnded) return;
    
        if (isPaused)
        {
            // 暂停状态下的输入处理
            if (Input.GetKeyDown(keyConfig.primaryKey) && waitingForInput)
            {
                // A键在暂停状态下只有在期望键匹配时才成功
                if (expectedKey == keyConfig.primaryKey || expectedKey == keyConfig.secondaryKey)
                {
                    OnBeatSuccess();
                }
            }
        }
        else
        {
            // 正常状态下的输入处理
            if (Input.GetKeyDown(keyConfig.primaryKey))
                OnKeyPressed(keyConfig.primaryKey);
        }
    }

    // 重写StartNextBeat：只使用主键prefab，但期望键仍然交替
    protected override void StartNextBeat()
    {
        if (pausedByManager) return;
        
        Debug.Log("NextBeat");
        expectedKey = (beatCounter % 2 == 0) ? keyConfig.primaryKey : keyConfig.secondaryKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;
        
        // 只设置主键颜色，因为只有一个prefab
        SetKeyColor(keyConfig.primaryKey, highlightKeyColor);
        beatCounter++;
    }

    // 重写OnKeyPressed：A键输入匹配A或D都成功
    protected override void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            if (pressedKey == keyConfig.primaryKey)
                ShowFeedback(keyConfig.primaryKey, missKeyColor);
            return;
        }

        // A键输入时总是成功，无论期望键是A还是D
        if (pressedKey == keyConfig.primaryKey)
            OnBeatSuccess();
    }

    // 添加成功事件触发
    protected override void OnBeatSuccess()
    {
        base.OnBeatSuccess();
        onADKeySucceeded?.Invoke();
    }

    // 完全按照ADController的模式处理失败
    protected override void OnBeatFailed()
    {
        pauseManager.scriptsToPause.Remove(this);
        base.OnBeatFailed();
        Debug.Log($"[{keyConfigPrefix}] 失败次数增加: {failCount}");
        onADKeyFailed?.Invoke();
    }

    protected override void OnGameFail()
    {
        SceneManager.LoadScene("Fail");
    }

    // 重写SetKeyColor：只处理主键
    // 修改 SetKeyColor 方法
    protected override void SetKeyColor(KeyCode key, Color color)
    {
        if (primaryKeySpriteRenderer != null)
        {
            if (primaryKeyColorCoroutine != null)
                StopCoroutine(primaryKeyColorCoroutine);

            // 暂停状态下的特殊处理
            if (isPaused && key == expectedKey && color == normalKeyColor)
            {
                if (waitingForInput)
                    primaryKeySpriteRenderer.color = highlightKeyColor;
                else
                    primaryKeySpriteRenderer.color = missKeyColor;
            }
            else
            {
                primaryKeySpriteRenderer.color = color;
            }
        }
    }

    // 重写ShowFeedback：只显示主键反馈
    protected override void ShowFeedback(KeyCode key, Color color)
    {
        if (primaryKeyColorCoroutine != null) StopCoroutine(primaryKeyColorCoroutine);
        primaryKeyColorCoroutine = StartCoroutine(ShowColorFeedback(primaryKeySpriteRenderer, color));
    }
    
    
}