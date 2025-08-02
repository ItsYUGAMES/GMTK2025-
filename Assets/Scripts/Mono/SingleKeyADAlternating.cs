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

        // 只设置主键sprite，因为只有一个prefab
        SetKeySprite(keyConfig.primaryKey, highlightKeySprite);
        beatCounter++;
    }

    // 重写OnKeyPressed：A键输入匹配A或D都成功
    protected override void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            if (pressedKey == keyConfig.primaryKey)
                ShowFeedback(keyConfig.primaryKey, missKeySprite);
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

    // 重写SetKeySprite：只处理主键
    protected override void SetKeySprite(KeyCode key, Sprite sprite)
    {
        if (primaryKeySpriteRenderer != null)
        {
            if (primaryKeySpriteCoroutine != null)
                StopCoroutine(primaryKeySpriteCoroutine);

            // 暂停状态下的特殊处理
            if (isPaused && key == expectedKey && sprite == normalKeySprite)
            {
                if (waitingForInput)
                    primaryKeySpriteRenderer.sprite = highlightKeySprite;
                else
                    primaryKeySpriteRenderer.sprite = missKeySprite;
            }
            else
            {
                primaryKeySpriteRenderer.sprite = sprite;
            }
        }
    }

    // 重写ShowFeedback：只显示主键反馈
    protected override void ShowFeedback(KeyCode key, Sprite sprite)
    {
        if (primaryKeySpriteCoroutine != null) StopCoroutine(primaryKeySpriteCoroutine);
        primaryKeySpriteCoroutine = StartCoroutine(ShowSpriteFeedback(primaryKeySpriteRenderer, sprite));
    }
}