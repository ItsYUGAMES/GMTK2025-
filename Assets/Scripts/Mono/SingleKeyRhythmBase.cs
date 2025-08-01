using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 單鍵節拍控制器基類，實現單鍵音游的基礎功能
/// </summary>
public abstract class SingleKeyRhythmBase : MonoBehaviour
{
    [Header("按鍵設置")]
    public KeyCode gameKey = KeyCode.Space;
    public string keyConfigPrefix = "Player";

    [Header("Prefab設置")]
    public GameObject keyPrefab;
    public Vector3 keySpawnPosition;

    [Header("視覺設置")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("節拍設置")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    [Header("關卡/失敗設置")]
    public int successToPass = 10;
    public int failToLose = 5;
    public int needConsecutiveSuccessToResume = 4;

    // 狀態變量
    public int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;
    public bool isPaused = false;
    protected bool pausedByManager = false;   // 被PauseManager暫停
    protected int consecutiveSuccessOnFailedKey = 0;

    // 運行時變量
    protected SpriteRenderer keySpriteRenderer;
    protected Camera mainCamera;
    protected Color originalCameraColor;
    protected int beatCounter = 0;
    protected bool waitingForInput = false;
    protected float currentBeatStartTime;
    protected Coroutine keyColorCoroutine;
    protected Coroutine beatCoroutine;

    protected Animator[] allAnimators;
    protected float[] originalAnimatorSpeeds;

    public Action OnLevelPassed;

    // ========== 生命週期 ==========
    protected virtual void Awake()
    {
        mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera != null) originalCameraColor = mainCamera.backgroundColor;

        if (keyPrefab != null)
        {
            GameObject obj = Instantiate(keyPrefab, keySpawnPosition, Quaternion.identity);
            keySpriteRenderer = obj.GetComponent<SpriteRenderer>();
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
        SetKeyColor(normalKeyColor);
        StartNextBeat();
    }

    // ========== 暫停控制 ==========
    public void SetPaused(bool paused)
    {
        pausedByManager = paused;
        if (paused)
            PauseAllAnimations();
        else
            ResumeAllAnimations();
    }

    // ========== 主循環 ==========
    protected virtual void Update()
    {
        if (pausedByManager) return;
        if (isGameEnded) return;

        CheckBeatTiming();
        HandleKeyInput();

        // 暫停狀態下也處理恢復輸入
        if (isPaused)
        {
            HandlePauseRecoveryInput();
        }
    }

    // ========== 輸入處理 ==========
    protected virtual void HandleKeyInput()
    {
        if (isGameEnded) return;

        if (Input.GetKeyDown(gameKey))
            OnKeyPressed();
    }

    protected virtual void HandlePauseRecoveryInput()
    {
        if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
        {
            ResumeFromPause();
        }
    }

    // ========== 節拍控制 ==========
    protected virtual void StartNextBeat()
    {
        if (pausedByManager) return;  // 只阻止管理器暫停，允許失敗暫停繼續節拍

        Debug.Log("NextBeat");
        currentBeatStartTime = Time.time;
        waitingForInput = true;
        SetKeyColor(highlightKeyColor);
        beatCounter++;

        // 檢查通關條件
        if (successCount >= successToPass)
        {
            isGameEnded = true;
            OnGameSuccess();
            return;
        }
    }

    protected virtual void CheckBeatTiming()
    {
        if (isGameEnded || !waitingForInput) return;
        float elapsed = Time.time - currentBeatStartTime;
        if (elapsed > successWindow)
            OnBeatMissed();
    }

    protected virtual void OnKeyPressed()
    {
        if (!waitingForInput)
        {
            ShowFeedback(missKeyColor);
            return;
        }

        OnBeatSuccess();
    }

    protected virtual void OnBeatSuccess()
    {
        if (isGameEnded) return;

        successCount++;
        waitingForInput = false;
        ShowFeedback(successKeyColor);

        // 如果在暫停狀態下成功，增加恢復計數
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey++;
            Debug.Log($"暫停恢復進度: {consecutiveSuccessOnFailedKey}/{needConsecutiveSuccessToResume}");

            // 如果達到恢復要求，恢復遊戲
            if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
                return; // 恢復後會重啟協程，所以這裡直接返回
            }
        }

        // 正常重啟協程
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        failCount++;
        waitingForInput = false;
        ShowFeedback(missKeyColor);

        // 如果在暫停狀態下失敗，重置恢復計數
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey = 0;
            Debug.Log("暫停狀態下失敗，重置恢復進度");
        }
        else
        {
            // 只有在非暫停狀態下才進入暫停
            EnterPauseForFailure();
        }

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        // 重啟協程
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void EnterPauseForFailure()
    {
        isPaused = true;
        consecutiveSuccessOnFailedKey = 0;

        // 不暫停動畫，讓一切正常運行
        // PauseAllAnimations();

        SetKeyColor(missKeyColor);
        Debug.Log($"[{keyConfigPrefix}] 進入暫停狀態，需要連按 {gameKey} 鍵 {needConsecutiveSuccessToResume} 次恢復");
    }

    protected virtual void ResumeFromPause()
    {
        isPaused = false;
        consecutiveSuccessOnFailedKey = 0;

        // 不需要恢復動畫，因為沒有暫停
        // ResumeAllAnimations();

        SetKeyColor(normalKeyColor);

        // 正常的協程重啟邏輯保持不變
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
        Debug.Log($"[{keyConfigPrefix}] 通關成功！");
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}] 遊戲失敗！");
    }

    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetKeyColor(normalKeyColor);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
    }

    // ========== 視覺反饋系統 ==========
    protected void ShowFeedback(Color color)
    {
        if (keyColorCoroutine != null) StopCoroutine(keyColorCoroutine);
        keyColorCoroutine = StartCoroutine(ShowColorFeedback(keySpriteRenderer, color));
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

        // 修改顏色恢復邏輯
        if (waitingForInput)
        {
            // 當前節拍的鍵始終顯示高亮
            renderer.color = highlightKeyColor;
        }
        else if (isPaused)
        {
            // 暫停狀態顯示紅色
            renderer.color = missKeyColor;
        }
        else
        {
            // 其他情況顯示正常顏色
            renderer.color = normalKeyColor;
        }
    }

    protected void SetKeyColor(Color color)
    {
        if (keySpriteRenderer != null)
        {
            if (keyColorCoroutine != null)
                StopCoroutine(keyColorCoroutine);

            // 暫停狀態下的特殊處理
            if (isPaused && color == normalKeyColor)
            {
                // 如果是要設置為正常顏色，檢查是否應該高亮
                if (waitingForInput)
                    keySpriteRenderer.color = highlightKeyColor;  // 當前節拍保持高亮
                else
                    keySpriteRenderer.color = missKeyColor;       // 暫停狀態保持紅色
            }
            else
            {
                keySpriteRenderer.color = color;
            }
        }
    }

    // ========== 公共方法 ==========
    public virtual void StartRhythm()
    {
        if (isGameEnded) return;
        SetKeyColor(normalKeyColor);
        StartNextBeat();
    }

    public virtual void ResetGame()
    {
        successCount = 0;
        failCount = 0;
        isGameEnded = false;
        isPaused = false;
        pausedByManager = false;
        consecutiveSuccessOnFailedKey = 0;
        waitingForInput = false;
        beatCounter = 0;

        if (beatCoroutine != null)
        {
            StopCoroutine(beatCoroutine);
            beatCoroutine = null;
        }

        if (keyColorCoroutine != null)
        {
            StopCoroutine(keyColorCoroutine);
            keyColorCoroutine = null;
        }

        SetKeyColor(normalKeyColor);
    }

    // ========== 狀態查詢 ==========
    public bool IsGameEnded() => isGameEnded;
    public bool IsGamePaused() => isPaused || pausedByManager;
    public int GetSuccessCount() => successCount;
    public int GetFailCount() => failCount;
    public int GetBeatCounter() => beatCounter;
}