using UnityEngine;
using System.Collections;

public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("键位配置")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player"; // 用于PlayerPrefs区分

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

    [Header("慢动作参数")]
    public float slowMotionDuration = 1.5f;
    public float slowMotionTimeScale = 0.3f;
    public Color slowMotionTintColor = Color.blue;
    public GameObject slowMotionIndicator;

    [Header("关卡/结局设置")]
    public int successToPass = 10;   // 通关所需成功次数
    public int failToLose = 5;       // 失败即判定为游戏失败

    protected int successCount = 0;  // 当前成功次数
    protected int failCount = 0;     // 当前失败次数
    protected bool isGameEnded = false; // 是否已结局

    // 运行时
    protected SpriteRenderer primaryKeySpriteRenderer;
    protected SpriteRenderer secondaryKeySpriteRenderer;
    protected float normalTimeScale = 1f;
    protected float slowMotionTimer = 0f;
    protected bool inSlowMotion = false;
    protected Camera mainCamera;
    protected Color originalCameraColor;
    protected int beatCounter = 0;
    protected bool waitingForInput = false;
    protected KeyCode expectedKey;
    protected float currentBeatStartTime;
    protected Coroutine primaryKeyColorCoroutine;
    protected Coroutine secondaryKeyColorCoroutine;
    protected Coroutine blinkCoroutine;

    protected virtual void Awake()
    {
        keyConfig.Load(keyConfigPrefix);

        normalTimeScale = Time.timeScale;
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
    }

    protected virtual void Start()
    {
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

    protected virtual void Update()
    {
        if (isGameEnded) return;
        HandleSlowMotion();
        CheckBeatTiming();
        HandlePlayerInput();
    }

    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded) return; // 结局后不响应输入

        if (Input.GetKeyDown(keyConfig.primaryKey))
            OnKeyPressed(keyConfig.primaryKey);
        else if (Input.GetKeyDown(keyConfig.secondaryKey))
            OnKeyPressed(keyConfig.secondaryKey);
    }

    protected virtual void StartNextBeat()
    {
        expectedKey = (beatCounter % 2 == 0) ? keyConfig.primaryKey : keyConfig.secondaryKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;
        SetKeyColor(expectedKey, highlightKeyColor);
        beatCounter++;
    }

    protected virtual void CheckBeatTiming()
    {
        if (isGameEnded) return;
        if (!waitingForInput) return;
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
            ShowFeedback(pressedKey, missKeyColor);
            return;
        }
        float responseTime = Time.time - currentBeatStartTime;
        if (pressedKey == expectedKey)
        {
            OnBeatSuccess();
        }
        else
        {
            OnBeatFailed();
        }
    }

    protected virtual void OnBeatSuccess()
    {
        if (isGameEnded) return;

        successCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);

        // 检查是否达成通关条件
        if (successCount >= successToPass)
        {
            isGameEnded = true;
            OnGameSuccess();
            return;
        }

        StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        failCount++;
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 检查是否失败
        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        StartCoroutine(WaitForNextBeat());
    }

    // 错过节拍也算失败，直接复用
    protected virtual void OnBeatMissed()
    {
        OnBeatFailed();
    }
    // 成功通关结局
    protected virtual void OnGameSuccess()
    {
        Debug.Log(" 通关成功！可在此加载下一关、显示通关界面、奖励结算等。");
        // 例如：SceneManager.LoadScene("NextLevel");
    }

    // 游戏失败结局
    protected virtual void OnGameFail()
    {
        Debug.Log(" 游戏失败！可在此显示失败界面、重试按钮等。");
        // 例如：SceneManager.LoadScene("GameOver");
    }


    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSeconds(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSeconds(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
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
        renderer.color = feedbackColor;
        yield return new WaitForSeconds(feedbackDisplayDuration);
        renderer.color = normalKeyColor;
    }

    protected void SetKeyColor(KeyCode key, Color color)
    {
        if (key == keyConfig.primaryKey && primaryKeySpriteRenderer != null)
        {
            if (primaryKeyColorCoroutine != null) StopCoroutine(primaryKeyColorCoroutine);
            primaryKeySpriteRenderer.color = color;
        }
        else if (key == keyConfig.secondaryKey && secondaryKeySpriteRenderer != null)
        {
            if (secondaryKeyColorCoroutine != null) StopCoroutine(secondaryKeyColorCoroutine);
            secondaryKeySpriteRenderer.color = color;
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

    // 慢动作
    protected virtual void HandleSlowMotion()
    {
        if (inSlowMotion)
        {
            slowMotionTimer += Time.unscaledDeltaTime;
            if (slowMotionTimer >= slowMotionDuration)
            {
                EndSlowMotion();
            }
        }
    }

    protected virtual void StartSlowMotion()
    {
        if (!inSlowMotion)
        {
            Time.timeScale = slowMotionTimeScale;
            inSlowMotion = true;
            slowMotionTimer = 0f;
            if (slowMotionIndicator != null) slowMotionIndicator.SetActive(true);
            if (mainCamera != null) mainCamera.backgroundColor = slowMotionTintColor;
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkAllKeys());
        }
    }

    protected virtual void EndSlowMotion()
    {
        Time.timeScale = normalTimeScale;
        inSlowMotion = false;
        slowMotionTimer = 0f;
        if (slowMotionIndicator != null) slowMotionIndicator.SetActive(false);
        if (mainCamera != null) mainCamera.backgroundColor = originalCameraColor;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        SetAllKeysColor(normalKeyColor);
    }

    protected IEnumerator BlinkAllKeys()
    {
        while (inSlowMotion)
        {
            SetAllKeysColor(missKeyColor);
            yield return new WaitForSecondsRealtime(0.2f);
            SetAllKeysColor(normalKeyColor);
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    // ---- 可选：暴露重置键位方法 ----
    public void ResetToDefaultKey()
    {
        keyConfig = new KeyConfig();
        keyConfig.Save(keyConfigPrefix);
    }

    // 在类的结尾加上：
    protected bool isReadyToStart = false;

    public virtual void StartRhythm()
    {
        if (isReadyToStart) return; // 防止重复启动
        isReadyToStart = true;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

}
