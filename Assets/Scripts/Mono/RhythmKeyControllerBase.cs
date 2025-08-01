using UnityEngine;
using System;
using System.Collections;

public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("键位配置")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player";

    [Header("Prefab配置")]
    public GameObject primaryKeyPrefab;
    public GameObject secondaryKeyPrefab;
    public Vector3 primaryKeySpawnPosition;
    public Vector3 secondaryKeySpawnPosition;

    [Header("视觉参数")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("节奏参数")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    [Header("关卡/结局设置")]
    public int successToPass = 10;
    public int failToLose = 5;

    protected int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;

    // 运行时
    protected SpriteRenderer primaryKeySpriteRenderer;
    protected SpriteRenderer secondaryKeySpriteRenderer;
    protected Camera mainCamera;
    protected Color originalCameraColor;
    protected int beatCounter = 0;
    protected bool waitingForInput = false;
    protected KeyCode expectedKey;
    protected float currentBeatStartTime;
    protected Coroutine primaryKeyColorCoroutine;
    protected Coroutine secondaryKeyColorCoroutine;
    protected Coroutine beatCoroutine;

    // 暂停解锁机制
    protected bool isInPauseForFailure = false;
    protected int consecutiveSuccessCount = 0;
    protected KeyCode nextExpectedKeyForRecovery;
    public int needConsecutiveSuccessToResume = 4;

    // 静态变量：记录哪个控制器触发了暂停
    protected static RhythmKeyControllerBase pauseOwner = null;

    // 动画相关
    protected Animator[] allAnimators;
    protected float[] originalAnimatorSpeeds;

    public Action OnLevelPassed;

    protected virtual void Awake()
    {
        keyConfig.Load(keyConfigPrefix);

        mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera != null) originalCameraColor = mainCamera.backgroundColor;

        if (primaryKeyPrefab != null)
        {
            GameObject obj = Instantiate(primaryKeyPrefab, primaryKeySpawnPosition, Quaternion.identity);
            primaryKeySpriteRenderer = obj.GetComponent<SpriteRenderer>();
        }
        if (secondaryKeyPrefab != null)
        {
            GameObject obj = Instantiate(secondaryKeyPrefab, secondaryKeySpawnPosition, Quaternion.identity);
            secondaryKeySpriteRenderer = obj.GetComponent<SpriteRenderer>();
        }

        // 收集场景中所有动画器
        allAnimators = FindObjectsOfType<Animator>();
        originalAnimatorSpeeds = new float[allAnimators.Length];
        for (int i = 0; i < allAnimators.Length; i++)
        {
            originalAnimatorSpeeds[i] = allAnimators[i].speed;
        }
    }

    protected virtual void Start()
    {
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

    protected virtual void Update()
    {
        if (isGameEnded) return;

        // 如果游戏暂停了
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            // 只有触发暂停的控制器才能处理恢复输入
            if (pauseOwner == this && isInPauseForFailure)
            {
                HandlePauseRecoveryInput();
            }
            // 其他控制器什么都不做
            return;
        }

        CheckBeatTiming();
        HandlePlayerInput();
    }

    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded || isInPauseForFailure) return;

        // 只检测这个控制器配置的按键
        if (Input.GetKeyDown(keyConfig.primaryKey))
        {
            OnKeyPressed(keyConfig.primaryKey);
        }
        else if (Input.GetKeyDown(keyConfig.secondaryKey))
        {
            OnKeyPressed(keyConfig.secondaryKey);
        }
    }

    protected virtual void HandlePauseRecoveryInput()
    {
        // 确保只有暂停的所有者才能处理输入
        if (pauseOwner != this) return;

        // 在暂停恢复阶段，需要按对A-D-A-D这样的序列
        if (Input.GetKeyDown(nextExpectedKeyForRecovery))
        {
            consecutiveSuccessCount++;
            ShowFeedback(nextExpectedKeyForRecovery, successKeyColor);

            // 切换到下一个期待的键
            nextExpectedKeyForRecovery = (nextExpectedKeyForRecovery == keyConfig.primaryKey)
                ? keyConfig.secondaryKey : keyConfig.primaryKey;

            // 高亮下一个需要按的键
            SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

            Debug.Log($"[{keyConfigPrefix}] 暂停恢复进度: {consecutiveSuccessCount}/{needConsecutiveSuccessToResume}");

            if (consecutiveSuccessCount >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
            }
        }
        else if (Input.GetKeyDown(keyConfig.primaryKey) || Input.GetKeyDown(keyConfig.secondaryKey))
        {
            // 按错了顺序，重置
            consecutiveSuccessCount = 0;
            ShowFeedback(Input.GetKeyDown(keyConfig.primaryKey) ? keyConfig.primaryKey : keyConfig.secondaryKey, missKeyColor);

            // 重新从第一个键开始
            nextExpectedKeyForRecovery = keyConfig.primaryKey;
            SetAllKeysColor(normalKeyColor);
            SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

            Debug.Log($"[{keyConfigPrefix}] 按错顺序，重置恢复进度");
        }
    }

    protected virtual void StartNextBeat()
    {
        if (isInPauseForFailure) return;

        expectedKey = (beatCounter % 2 == 0) ? keyConfig.primaryKey : keyConfig.secondaryKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;
        SetKeyColor(expectedKey, highlightKeyColor);
        beatCounter++;
    }

    protected virtual void CheckBeatTiming()
    {
        if (isGameEnded || !waitingForInput) return;
        float elapsed = Time.time - currentBeatStartTime;
        if (elapsed > successWindow)
        {
            OnBeatMissed();
        }
    }

    protected virtual void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            // 即使不在等待输入窗口，也只对配置的按键显示反馈
            if (pressedKey == keyConfig.primaryKey || pressedKey == keyConfig.secondaryKey)
            {
                ShowFeedback(pressedKey, missKeyColor);
            }
            return;
        }

        if (pressedKey == expectedKey)
        {
            OnBeatSuccess();
        }
        else if (pressedKey == keyConfig.primaryKey || pressedKey == keyConfig.secondaryKey)
        {
            // 只有按了本控制器的键但按错了才算失败
            OnBeatFailed();
        }
    }

    protected virtual void OnBeatSuccess()
    {
        if (isGameEnded) return;

        successCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);

        if (successCount >= successToPass)
        {
            isGameEnded = true;
            OnGameSuccess();
            return;
        }

        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        failCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);

        // 进入暂停状态
        EnterPauseForFailure();

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
        }
    }

    protected virtual void EnterPauseForFailure()
    {
        isInPauseForFailure = true;
        consecutiveSuccessCount = 0;

        // 设置这个控制器为暂停的所有者
        pauseOwner = this;

        // 设置恢复序列从主键开始
        nextExpectedKeyForRecovery = keyConfig.primaryKey;

        // 停止节拍协程
        if (beatCoroutine != null)
        {
            StopCoroutine(beatCoroutine);
            beatCoroutine = null;
        }

        // 通过PauseManager暂停整个游戏
        if (PauseManager.Instance != null)
            PauseManager.Instance.SetPause(true);

        // 暂停所有动画
        PauseAllAnimations();

        // 显示第一个需要按的键
        SetAllKeysColor(normalKeyColor);
        SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

        Debug.Log($"[{keyConfigPrefix}] 进入暂停状态，需要按顺序输入 {keyConfig.primaryKey}-{keyConfig.secondaryKey} 共 {needConsecutiveSuccessToResume} 次恢复");
    }

    protected virtual void ResumeFromPause()
    {
        isInPauseForFailure = false;
        consecutiveSuccessCount = 0;

        // 清除暂停所有者
        pauseOwner = null;

        // 恢复所有动画
        ResumeAllAnimations();

        // 通过PauseManager恢复游戏
        if (PauseManager.Instance != null)
            PauseManager.Instance.SetPause(false);

        // 重置按键颜色
        SetAllKeysColor(normalKeyColor);

        Debug.Log($"[{keyConfigPrefix}] 恢复游戏");

        // 重新开始节拍
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void PauseAllAnimations()
    {
        // 暂停所有动画器
        for (int i = 0; i < allAnimators.Length; i++)
        {
            if (allAnimators[i] != null)
                allAnimators[i].speed = 0;
        }
    }

    protected virtual void ResumeAllAnimations()
    {
        // 恢复所有动画器
        for (int i = 0; i < allAnimators.Length; i++)
        {
            if (allAnimators[i] != null)
                allAnimators[i].speed = originalAnimatorSpeeds[i];
        }
    }

    protected virtual void OnBeatMissed()
    {
        OnBeatFailed();
    }

    protected virtual void OnGameSuccess()
    {
        Debug.Log($"[{keyConfigPrefix}] 通关成功！");
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}] 游戏失败！");
    }

    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSeconds(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSeconds(beatInterval - 0.1f);
        if (infiniteLoop && !isInPauseForFailure) StartNextBeat();
    }

    protected void ShowFeedback(KeyCode key, Color color)
    {
        if (key == keyConfig.primaryKey)
        {
            if (primaryKeyColorCoroutine != null) StopCoroutine(primaryKeyColorCoroutine);
            primaryKeyColorCoroutine = StartCoroutine(ShowColorFeedback(primaryKeySpriteRenderer, color));
        }
        else if (key == keyConfig.secondaryKey)
        {
            if (secondaryKeyColorCoroutine != null) StopCoroutine(secondaryKeyColorCoroutine);
            secondaryKeyColorCoroutine = StartCoroutine(ShowColorFeedback(secondaryKeySpriteRenderer, color));
        }
    }

    protected IEnumerator ShowColorFeedback(SpriteRenderer renderer, Color feedbackColor)
    {
        if (renderer == null) yield break;
        Color originalColor = renderer.color;
        renderer.color = feedbackColor;

        // 在暂停状态下使用真实时间
        if (isInPauseForFailure)
        {
            float elapsedTime = 0;
            while (elapsedTime < feedbackDisplayDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(feedbackDisplayDuration);
        }

        // 恢复颜色逻辑
        if (isInPauseForFailure && renderer == GetKeyRenderer(nextExpectedKeyForRecovery))
        {
            // 如果是下一个期待的键，保持高亮
            renderer.color = highlightKeyColor;
        }
        else
        {
            renderer.color = normalKeyColor;
        }
    }

    protected SpriteRenderer GetKeyRenderer(KeyCode key)
    {
        if (key == keyConfig.primaryKey) return primaryKeySpriteRenderer;
        if (key == keyConfig.secondaryKey) return secondaryKeySpriteRenderer;
        return null;
    }

    protected void SetKeyColor(KeyCode key, Color color)
    {
        SpriteRenderer renderer = GetKeyRenderer(key);
        if (renderer != null)
        {
            if (key == keyConfig.primaryKey && primaryKeyColorCoroutine != null)
                StopCoroutine(primaryKeyColorCoroutine);
            else if (key == keyConfig.secondaryKey && secondaryKeyColorCoroutine != null)
                StopCoroutine(secondaryKeyColorCoroutine);

            renderer.color = color;
        }
    }

    protected void SetAllKeysColor(Color color)
    {
        if (primaryKeySpriteRenderer != null)
        {
            if (primaryKeyColorCoroutine != null) StopCoroutine(primaryKeyColorCoroutine);
            primaryKeySpriteRenderer.color = color;
        }
        if (secondaryKeySpriteRenderer != null)
        {
            if (secondaryKeyColorCoroutine != null) StopCoroutine(secondaryKeyColorCoroutine);
            secondaryKeySpriteRenderer.color = color;
        }
    }

    public void ResetToDefaultKey()
    {
        keyConfig = new KeyConfig();
        keyConfig.Save(keyConfigPrefix);
    }

    protected bool isReadyToStart = false;

    public virtual void StartRhythm()
    {
        if (isReadyToStart) return;
        isReadyToStart = true;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

    protected virtual void OnDestroy()
    {
        // 确保恢复时间缩放
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused && isInPauseForFailure)
        {
            PauseManager.Instance.SetPause(false);
            pauseOwner = null;
        }
    }
}