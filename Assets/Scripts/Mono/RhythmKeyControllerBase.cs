using UnityEngine;
using System;
using System.Collections;

/// <summary>
///       ƻ  ࣬ʵ  IPausable ӿڣ ֧  PauseManagerȫ  / ֲ   ͣ    
/// </summary>
public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("  λ    ")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player";

    [Header("Prefab    ")]
    public GameObject primaryKeyPrefab;
    public GameObject secondaryKeyPrefab;
    public Vector3 primaryKeySpawnPosition;
    public Vector3 secondaryKeySpawnPosition;

    [Header(" Ӿ     ")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("       ")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    // 修改字段声明部分
    [Header("关卡/失败设置")]
    public int successToPass = 10;
    public int failToLose = 5;
    public int needConsecutiveSuccessToResume = 4;

   
    // ״̬  
    public int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;
    public bool isPaused = false;
    protected bool pausedByManager = false;   //   PauseManager  ͣ
    public int consecutiveSuccessOnFailedKey = 0;

    //     ʱ
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

    // ==========          ==========
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

        allAnimators = FindObjectsOfType<Animator>();
        originalAnimatorSpeeds = new float[allAnimators.Length];
        for (int i = 0; i < allAnimators.Length; i++)
            originalAnimatorSpeeds[i] = allAnimators[i].speed;
    }

    protected virtual void OnDestroy()
    {
        
    }

    protected virtual void Start()
    {
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

    // ========== IPausableʵ   ==========
    public void SetPaused(bool paused)
    {
        pausedByManager = paused;
        //    趯  ͬ     ᣬ       ﴦ   allAnimators
        if (paused)
            PauseAllAnimations();
        else
            ResumeAllAnimations();
    }

    // ==========   ѭ   ==========
    protected virtual void Update()
    {
        if (pausedByManager) return;
        if (isGameEnded) return;

        // 移除暂停状态的特殊处理，让脚本正常运行
        // if (isPaused)
        // {
        //     HandlePauseRecoveryInput();
        //     CheckBeatTiming();
        //     return;
        // }

        CheckBeatTiming();
        HandlePlayerInput();
    
        // 暂停状态下也处理恢复输入
        if (isPaused)
        {
            HandlePauseRecoveryInput();
        }
    }

    // ==========         ==========
    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded) return;
        // 移除 isPaused 检查，让暂停状态下也能正常输入

        if (Input.GetKeyDown(keyConfig.primaryKey))
            OnKeyPressed(keyConfig.primaryKey);
        else if (Input.GetKeyDown(keyConfig.secondaryKey))
            OnKeyPressed(keyConfig.secondaryKey);
    }

    protected virtual void HandlePauseRecoveryInput()
    {
        // 暂停状态下只响应当前期望的键
        if (Input.GetKeyDown(expectedKey) && waitingForInput)
        {
            OnBeatSuccess(); // 这会增加 consecutiveSuccessOnFailedKey 并检查是否达到恢复条件
        }
        else if ((Input.GetKeyDown(keyConfig.primaryKey) || Input.GetKeyDown(keyConfig.secondaryKey)) && waitingForInput)
        {
            // 按错键时重置恢复进度
            if (Input.GetKeyDown(expectedKey) == false)
            {
                consecutiveSuccessOnFailedKey = 0;
                OnBeatFailed();
            }
        }
    }

    protected bool IsOtherControllerKeyPressed()
    {
        KeyCode otherKey = (expectedKey == keyConfig.primaryKey) ? keyConfig.secondaryKey : keyConfig.primaryKey;
        return Input.GetKeyDown(otherKey);
    }

  
    protected virtual void StartNextBeat()
    {
        if (pausedByManager) return;  // 只阻止管理器暂停，允许失败暂停继续节拍
    
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

        // 如果在暂停状态下成功，增加恢复计数
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey++;
            Debug.Log($"暂停恢复进度: {consecutiveSuccessOnFailedKey}/{needConsecutiveSuccessToResume}");
        
            // 如果达到恢复要求，恢复游戏
            if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
                return; // 恢复后会重启协程，所以这里直接返回
            }
        }

        // 正常重启协程
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        if(!isPaused)failCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);

        // 如果在暂停状态下失败，重置恢复计数
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey = 0;
            Debug.Log("暂停状态下失败，重置恢复进度");
        }
        else
        {
            // 只有在非暂停状态下才进入暂停
            EnterPauseForFailure();
        }

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        // 重启协程
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void EnterPauseForFailure()
    {
        isPaused = true;
        consecutiveSuccessOnFailedKey = 0;

        SetKeyColor(expectedKey, missKeyColor); // 使用 expectedKey 替代 lastFailedKey
        Debug.Log($"[{keyConfigPrefix}] 进入暂停状态，需要连按 {expectedKey} 键 {needConsecutiveSuccessToResume} 次恢复");
    }

    protected virtual void ResumeFromPause()
    {
        Debug.Log("恢复游戏状态");
        isPaused = false;
        consecutiveSuccessOnFailedKey = 0;
    
        // 不需要恢复动画，因为没有暂停
        // ResumeAllAnimations();
    
        SetAllKeysColor(normalKeyColor);

        // 正常的协程重启逻辑保持不变
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

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

    protected virtual void OnBeatMissed()
    {
        OnBeatFailed();
    }

    protected virtual void OnGameSuccess()
    {
        Debug.Log($"[{keyConfigPrefix}] ͨ سɹ   ");
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}]   Ϸʧ ܣ ");
    }

    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
    }

    // ==========  Ӿ         ==========
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

        // 修改颜色恢复逻辑 - 优先显示当前节拍高亮
        if (renderer == GetKeyRenderer(expectedKey) && waitingForInput)
        {
            renderer.color = highlightKeyColor;
        }
        else if (isPaused && renderer == GetKeyRenderer(expectedKey))
        {
            renderer.color = missKeyColor; // 使用 expectedKey 替代 lastFailedKey
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

            // 暂停状态下的特殊处理
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
