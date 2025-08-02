using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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

    [Header("Sprite设置")]
    public Sprite normalKeySprite;
    public Sprite highlightKeySprite;
    public Sprite successKeySprite;
    public Sprite missKeySprite;
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
    private int baseFailToLose;
    private float baseSuccessWindow;
    private float baseBeatInterval;

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
    protected Coroutine primaryKeySpriteCoroutine;
    protected Coroutine secondaryKeySpriteCoroutine;
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
        ApplyPendingItemEffects();

        if (PlayerPrefs.GetInt("HoldModeEnabled", 0) == 1)
        {
            EnableHoldMode(PlayerPrefs.GetFloat("HoldDuration", 0.5f));
        }

        if (PlayerPrefs.GetInt("AutoPlayEnabled", 0) == 1)
        {
            EnableAutoPlay(PlayerPrefs.GetFloat("AutoPlayAccuracy", 0.95f));
        }

        SetAllKeysSprite(normalKeySprite);
        StartNextBeat();
    }

    protected virtual void OnDestroy()
    {
        // 清理资源
    }

    // ========== 道具效果应用 ==========
    private void ApplyPendingItemEffects()
    {
        int pendingExtraLives = PlayerPrefs.GetInt("PendingExtraLives", 0);
        if (pendingExtraLives > 0)
        {
            failToLose += pendingExtraLives;
            Debug.Log($"[{keyConfigPrefix}] 应用了 {pendingExtraLives} 个待处理的额外生命，当前失败上限: {failToLose}");
            PlayerPrefs.SetInt("PendingExtraLives", 0);
            PlayerPrefs.Save();
        }

        float windowBonus = PlayerPrefs.GetFloat("SuccessWindowBonus", 0f);
        if (windowBonus > 0)
        {
            successWindow += windowBonus;
            Debug.Log($"[{keyConfigPrefix}] 应用了成功窗口加成 {windowBonus} 秒，当前成功窗口: {successWindow}");
        }

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
        if (isAutoPlay) return;

        if (isHoldMode)
        {
            HandleHoldInput();
        }
        else
        {
            if (Input.GetKeyDown(keyConfig.primaryKey))
                OnKeyPressed(keyConfig.primaryKey);
            else if (Input.GetKeyDown(keyConfig.secondaryKey))
                OnKeyPressed(keyConfig.secondaryKey);
        }
    }

    private void HandleHoldInput()
    {
        if (Input.GetKeyDown(keyConfig.primaryKey) && !isHolding && expectedKey == keyConfig.primaryKey)
        {
            isHolding = true;
            holdStartTime = Time.time;
            currentHoldKey = keyConfig.primaryKey;
            ShowFeedback(keyConfig.primaryKey, highlightKeySprite);
        }

        if (Input.GetKeyDown(keyConfig.secondaryKey) && !isHolding && expectedKey == keyConfig.secondaryKey)
        {
            isHolding = true;
            holdStartTime = Time.time;
            currentHoldKey = keyConfig.secondaryKey;
            ShowFeedback(keyConfig.secondaryKey, highlightKeySprite);
        }

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
                ShowFeedback(currentHoldKey, missKeySprite);
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
        SetKeySprite(expectedKey, highlightKeySprite);
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
                ShowFeedback(pressedKey, missKeySprite);
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
        ShowFeedback(expectedKey, successKeySprite);

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
        ShowFeedback(expectedKey, missKeySprite);

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
        SetKeySprite(expectedKey, missKeySprite);
        Debug.Log($"[{keyConfigPrefix}] 进入暂停状态，需要连按 {expectedKey} 键 {needConsecutiveSuccessToResume} 次恢复");
    }

    protected virtual void ResumeFromPause()
    {
        Debug.Log("恢复游戏状态");
        isPaused = false;
        consecutiveSuccessOnFailedKey = 0;
        SetAllKeysSprite(normalKeySprite);
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
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
        SceneManager.LoadScene("Fail");
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
                float waitTime = Random.Range(successWindow * 0.2f, successWindow * 0.8f);
                yield return new WaitForSeconds(waitTime);

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
        SetAllKeysSprite(normalKeySprite);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
    }

    // ========== 视觉反馈（Sprite版本）==========
    protected virtual void ShowFeedback(KeyCode key, Sprite sprite)
    {
        if (key == keyConfig.primaryKey)
        {
            if (primaryKeySpriteCoroutine != null) StopCoroutine(primaryKeySpriteCoroutine);
            primaryKeySpriteCoroutine = StartCoroutine(ShowSpriteFeedback(primaryKeySpriteRenderer, sprite));
        }
        else if (key == keyConfig.secondaryKey)
        {
            if (secondaryKeySpriteCoroutine != null) StopCoroutine(secondaryKeySpriteCoroutine);
            secondaryKeySpriteCoroutine = StartCoroutine(ShowSpriteFeedback(secondaryKeySpriteRenderer, sprite));
        }
    }

    protected IEnumerator ShowSpriteFeedback(SpriteRenderer renderer, Sprite feedbackSprite)
    {
        if (renderer == null) yield break;
        Sprite originalSprite = renderer.sprite;
        renderer.sprite = feedbackSprite;

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
            renderer.sprite = highlightKeySprite;
        }
        else if (isPaused && renderer == GetKeyRenderer(expectedKey))
        {
            renderer.sprite = missKeySprite;
        }
        else
        {
            renderer.sprite = normalKeySprite;
        }
    }

    protected SpriteRenderer GetKeyRenderer(KeyCode key)
    {
        if (key == keyConfig.primaryKey) return primaryKeySpriteRenderer;
        if (key == keyConfig.secondaryKey) return secondaryKeySpriteRenderer;
        return null;
    }

    protected virtual void SetKeySprite(KeyCode key, Sprite sprite)
    {
        SpriteRenderer renderer = GetKeyRenderer(key);
        if (renderer != null)
        {
            if (key == keyConfig.primaryKey && primaryKeySpriteCoroutine != null)
                StopCoroutine(primaryKeySpriteCoroutine);
            else if (key == keyConfig.secondaryKey && secondaryKeySpriteCoroutine != null)
                StopCoroutine(secondaryKeySpriteCoroutine);

            if (isPaused && key == expectedKey && sprite == normalKeySprite)
            {
                if (waitingForInput)
                    renderer.sprite = highlightKeySprite;
                else
                    renderer.sprite = missKeySprite;
            }
            else
            {
                renderer.sprite = sprite;
            }
        }
    }

    protected void SetAllKeysSprite(Sprite sprite)
    {
        if (primaryKeySpriteRenderer != null)
        {
            if (primaryKeySpriteCoroutine != null) StopCoroutine(primaryKeySpriteCoroutine);
            primaryKeySpriteRenderer.sprite = sprite;
        }
        if (secondaryKeySpriteRenderer != null)
        {
            if (secondaryKeySpriteCoroutine != null) StopCoroutine(secondaryKeySpriteCoroutine);
            secondaryKeySpriteRenderer.sprite = sprite;
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
        SetAllKeysSprite(normalKeySprite);
        StartNextBeat();
    }
}