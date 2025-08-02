using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random; // 明确使用 Unity 的 Random

/// <summary>
/// 基础节奏控制器，支持道具系统
/// </summary>
public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("按键配置")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player";

    [Header("Prefab设置")]
    public GameObject primaryKeyPrefab;
    public GameObject secondaryKeyPrefab;
    public Vector3 primaryKeySpawnPosition;
    public Vector3 secondaryKeySpawnPosition;

    [Header("视觉反馈")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("节奏设置")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    [Header("关卡/失败设置")]
    public int successToPass = 10;
    public int failToLose = 5;
    public int needConsecutiveSuccessToResume = 4;

    [Header("道具效果")]
    private int baseFailToLose;           // 储存原始的失败上限
    private float baseSuccessWindow;      // 储存原始的成功窗口
    private float baseBeatInterval;       // 储存原始的节拍间隔

    [Header("道具模式")]
    private bool isHoldMode = false;
    private float holdDuration = 0.5f;
    private bool isAutoPlay = false;
    private float autoPlayAccuracy = 0.95f;
    private float holdStartTime = 0f;
    private bool isHolding = false;
    private KeyCode currentHoldKey = KeyCode.None;

    // 状态变量
    public int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;
    public bool isPaused = false;
    protected bool pausedByManager = false;
    public int consecutiveSuccessOnFailedKey = 0;

    // 组件引用
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

    protected Animator[] allAnimators;
    protected float[] originalAnimatorSpeeds;

    public Action OnLevelPassed;

    // ========== 生命周期方法 ==========
    protected virtual void Awake()
    {
        keyConfig.Load(keyConfigPrefix);

        // 保存基础值
        baseFailToLose = failToLose;
        baseSuccessWindow = successWindow;
        baseBeatInterval = beatInterval;

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

        allAnimators = FindObjectsOfType<Animator>();
        originalAnimatorSpeeds = new float[allAnimators.Length];
        for (int i = 0; i < allAnimators.Length; i++)
            originalAnimatorSpeeds[i] = allAnimators[i].speed;
    }

    protected virtual void Start()
    {
        // 应用待处理的道具效果
        ApplyPendingItemEffects();

        // 检查是否启用了特殊模式
        if (PlayerPrefs.GetInt("HoldModeEnabled", 0) == 1)
        {
            EnableHoldMode(PlayerPrefs.GetFloat("HoldDuration", 0.5f));
        }

        if (PlayerPrefs.GetInt("AutoPlayEnabled", 0) == 1)
        {
            EnableAutoPlay(PlayerPrefs.GetFloat("AutoPlayAccuracy", 0.95f));
        }

        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

    protected virtual void OnDestroy()
    {
        // 清理资源
    }

    // ========== 道具效果应用 ==========
    private void ApplyPendingItemEffects()
    {
        // 应用额外生命效果
        int pendingExtraLives = PlayerPrefs.GetInt("PendingExtraLives", 0);
        if (pendingExtraLives > 0)
        {
            failToLose += pendingExtraLives;
            Debug.Log($"[{keyConfigPrefix}] 应用了 {pendingExtraLives} 个待处理的额外生命，当前失败上限: {failToLose}");

            PlayerPrefs.SetInt("PendingExtraLives", 0);
            PlayerPrefs.Save();
        }

        // 应用成功窗口加成
        float windowBonus = PlayerPrefs.GetFloat("SuccessWindowBonus", 0f);
        if (windowBonus > 0)
        {
            successWindow += windowBonus;
            Debug.Log($"[{keyConfigPrefix}] 应用了成功窗口加成 {windowBonus} 秒，当前成功窗口: {successWindow}");
        }

        // 应用速度效果
        float savedSpeedMultiplier = PlayerPrefs.GetFloat("SpeedMultiplier", 1.0f);
        if (savedSpeedMultiplier != 1.0f)
        {
            beatInterval = baseBeatInterval / savedSpeedMultiplier;
            Debug.Log($"[{keyConfigPrefix}] 应用了速度倍数 {savedSpeedMultiplier}，当前节拍间隔: {beatInterval}");
        }
    }

    // ========== IPausable实现 ==========
    public void SetPaused(bool paused)
    {
        pausedByManager = paused;
        if (paused)
            PauseAllAnimations();
        else
            ResumeAllAnimations();
    }

    // ========== 主循环 ==========
    protected virtual void Update()
    {
        if (pausedByManager) return;
        if (isGameEnded) return;

        CheckBeatTiming();
        HandlePlayerInput();

        if (isPaused)
        {
            HandlePauseRecoveryInput();
        }
    }

    // ========== 输入处理 ==========
    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded) return;
        if (isAutoPlay) return; // 自动模式下不处理玩家输入

        if (isHoldMode)
        {
            HandleHoldInput();
        }
        else
        {
            // 原有的按键逻辑
            if (Input.GetKeyDown(keyConfig.primaryKey))
                OnKeyPressed(keyConfig.primaryKey);
            else if (Input.GetKeyDown(keyConfig.secondaryKey))
                OnKeyPressed(keyConfig.secondaryKey);
        }
    }

    // 处理长按输入
    private void HandleHoldInput()
    {
        // 检测主键按下
        if (Input.GetKeyDown(keyConfig.primaryKey) && !isHolding && expectedKey == keyConfig.primaryKey)
        {
            isHolding = true;
            holdStartTime = Time.time;
            currentHoldKey = keyConfig.primaryKey;
            ShowFeedback(keyConfig.primaryKey, highlightKeyColor);
        }

        // 检测副键按下
        if (Input.GetKeyDown(keyConfig.secondaryKey) && !isHolding && expectedKey == keyConfig.secondaryKey)
        {
            isHolding = true;
            holdStartTime = Time.time;
            currentHoldKey = keyConfig.secondaryKey;
            ShowFeedback(keyConfig.secondaryKey, highlightKeyColor);
        }

        // 检测按键释放
        if (isHolding && Input.GetKeyUp(currentHoldKey))
        {
            isHolding = false;
            float holdTime = Time.time - holdStartTime;

            if (holdTime >= holdDuration && waitingForInput && currentHoldKey == expectedKey)
            {
                OnBeatSuccess();
            }
            else
            {
                ShowFeedback(currentHoldKey, missKeyColor);
                if (waitingForInput)
                {
                    OnBeatFailed();
                }
            }
            currentHoldKey = KeyCode.None;
        }
    }

    protected virtual void HandlePauseRecoveryInput()
    {
        if (Input.GetKeyDown(expectedKey) && waitingForInput)
        {
            OnBeatSuccess();
        }
        else if ((Input.GetKeyDown(keyConfig.primaryKey) || Input.GetKeyDown(keyConfig.secondaryKey)) && waitingForInput)
        {
            if (Input.GetKeyDown(expectedKey) == false)
            {
                consecutiveSuccessOnFailedKey = 0;
                OnBeatFailed();
            }
        }
    }

    // ========== 节奏控制 ==========
    protected virtual void StartNextBeat()
    {
        if (pausedByManager) return;

        Debug.Log("NextBeat");
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
            OnBeatMissed();
    }

    protected virtual void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            if (pressedKey == keyConfig.primaryKey || pressedKey == keyConfig.secondaryKey)
                ShowFeedback(pressedKey, missKeyColor);
            return;
        }

        if (pressedKey == expectedKey)
            OnBeatSuccess();
        else if (pressedKey == keyConfig.primaryKey || pressedKey == keyConfig.secondaryKey)
            OnBeatFailed();
    }

    protected virtual void OnBeatSuccess()
    {
        if (isGameEnded) return;

        successCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);

        if (isPaused)
        {
            consecutiveSuccessOnFailedKey++;
            Debug.Log($"暂停恢复进度: {consecutiveSuccessOnFailedKey}/{needConsecutiveSuccessToResume}");

            if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
                return;
            }
        }

        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        if (!isPaused) failCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);

        if (isPaused)
        {
            consecutiveSuccessOnFailedKey = 0;
            Debug.Log("暂停状态下失败，重置恢复进度");
        }
        else
        {
            EnterPauseForFailure();
        }

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatMissed()
    {
        OnBeatFailed();
    }

    // ========== 暂停/恢复 ==========
    protected virtual void EnterPauseForFailure()
    {
        isPaused = true;
        consecutiveSuccessOnFailedKey = 0;
        SetKeyColor(expectedKey, missKeyColor);
        Debug.Log($"[{keyConfigPrefix}] 进入暂停状态，需要连按 {expectedKey} 键 {needConsecutiveSuccessToResume} 次恢复");
    }

    protected virtual void ResumeFromPause()
    {
        Debug.Log("恢复游戏状态");
        isPaused = false;
        consecutiveSuccessOnFailedKey = 0;
        SetAllKeysColor(normalKeyColor);

        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    // ========== 游戏结束 ==========
    protected virtual void OnGameSuccess()
    {
        Debug.Log($"[{keyConfigPrefix}] 通过成功！");
        TriggerRoundEndEffects();
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}] 游戏失败！");
    }

    // ========== 道具系统公共方法 ==========
    public void AddExtraLife(int amount)
    {
        failToLose += amount;
        Debug.Log($"[{keyConfigPrefix}] 增加了 {amount} 次失误机会，当前失败上限: {failToLose}");
    }

    public int GetCurrentFailLimit() => failToLose;
    public int GetCurrentFailCount() => failCount;
    public int GetRemainingLives() => failToLose - failCount;

    public void SetSpeedMultiplier(float multiplier)
    {
        beatInterval = baseBeatInterval / multiplier;
        Debug.Log($"[{keyConfigPrefix}] 速度倍数设置为 {multiplier}，当前节拍间隔: {beatInterval}");
    }

    public void AddSuccessWindow(float additionalTime)
    {
        successWindow += additionalTime;
        Debug.Log($"[{keyConfigPrefix}] 成功窗口增加 {additionalTime} 秒，当前成功窗口: {successWindow}");
    }

    public void EnableHoldMode(float duration)
    {
        isHoldMode = true;
        holdDuration = duration;
        Debug.Log($"[{keyConfigPrefix}] 启用长按模式，需要按住 {duration} 秒");
    }

    public void EnableAutoPlay(float accuracy)
    {
        isAutoPlay = true;
        autoPlayAccuracy = accuracy;
        Debug.Log($"[{keyConfigPrefix}] 启用自动游玩，准确率: {accuracy * 100}%");
        StartCoroutine(AutoPlayRoutine());
    }

    public void ResetItemEffects()
    {
        failToLose = baseFailToLose;
        successWindow = baseSuccessWindow;
        beatInterval = baseBeatInterval;
        isHoldMode = false;
        isAutoPlay = false;
        Debug.Log($"[{keyConfigPrefix}] 所有道具效果已重置");
    }

    // ========== 自动游玩协程 ==========
    private IEnumerator AutoPlayRoutine()
    {
        while (isAutoPlay && !isGameEnded)
        {
            if (waitingForInput)
            {
                // 等待一个随机的时间（在成功窗口内）
                float waitTime = Random.Range(successWindow * 0.2f, successWindow * 0.8f);
                yield return new WaitForSeconds(waitTime);

                // 根据准确率决定是否成功
                if (Random.value < autoPlayAccuracy && waitingForInput)
                {
                    OnKeyPressed(expectedKey);
                }
            }
            yield return null;
        }
    }

    // ========== 回合结束效果 ==========
    private void TriggerRoundEndEffects()
    {
        // 处理金币倍数持续回合
        int remainingGoldRounds = PlayerPrefs.GetInt("GoldMultiplierRounds", 0);
        if (remainingGoldRounds > 0)
        {
            remainingGoldRounds--;
            PlayerPrefs.SetInt("GoldMultiplierRounds", remainingGoldRounds);

            if (remainingGoldRounds == 0)
            {
                PlayerPrefs.SetFloat("GoldMultiplier", 1.0f);
                Debug.Log("金币加成效果已结束");
            }
            else
            {
                Debug.Log($"金币加成剩余 {remainingGoldRounds} 回合");
            }

            PlayerPrefs.Save();
        }
    }

    // ========== 协程 ==========
    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
    }

    // ========== 视觉反馈 ==========
    protected virtual void ShowFeedback(KeyCode key, Color color)
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

        if (isPaused)
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
            yield return new WaitForSecondsRealtime(feedbackDisplayDuration);
        }

        if (renderer == GetKeyRenderer(expectedKey) && waitingForInput)
        {
            renderer.color = highlightKeyColor;
        }
        else if (isPaused && renderer == GetKeyRenderer(expectedKey))
        {
            renderer.color = missKeyColor;
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

    protected virtual void SetKeyColor(KeyCode key, Color color)
    {
        SpriteRenderer renderer = GetKeyRenderer(key);
        if (renderer != null)
        {
            if (key == keyConfig.primaryKey && primaryKeyColorCoroutine != null)
                StopCoroutine(primaryKeyColorCoroutine);
            else if (key == keyConfig.secondaryKey && secondaryKeyColorCoroutine != null)
                StopCoroutine(secondaryKeyColorCoroutine);

            if (isPaused && key == expectedKey && color == normalKeyColor)
            {
                if (waitingForInput)
                    renderer.color = highlightKeyColor;
                else
                    renderer.color = missKeyColor;
            }
            else
            {
                renderer.color = color;
            }
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

    // ========== 动画控制 ==========
    protected virtual void PauseAllAnimations()
    {
        for (int i = 0; i < allAnimators.Length; i++)
        {
            if (allAnimators[i] != null)
                allAnimators[i].speed = 0;
        }
    }

    protected virtual void ResumeAllAnimations()
    {
        for (int i = 0; i < allAnimators.Length; i++)
        {
            if (allAnimators[i] != null)
                allAnimators[i].speed = originalAnimatorSpeeds[i];
        }
    }

    // ========== 其他公共方法 ==========
    public void ResetToDefaultKey()
    {
        keyConfig = new KeyConfig();
        keyConfig.Save(keyConfigPrefix);
    }

    public virtual void StartRhythm()
    {
        if (isGameEnded) return;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }
}