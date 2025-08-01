using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ������ƻ��࣬ʵ��IPausable�ӿڣ�֧��PauseManagerȫ��/�ֲ���ͣ����
/// </summary>
public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("��λ����")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player";

    [Header("Prefab����")]
    public GameObject primaryKeyPrefab;
    public GameObject secondaryKeyPrefab;
    public Vector3 primaryKeySpawnPosition;
    public Vector3 secondaryKeySpawnPosition;

    [Header("�Ӿ�����")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("�������")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    // 修改字段声明部分
    [Header("关卡/失败设置")]
    public int successToPass = 10;
    public int failToLose = 5;
    public int needConsecutiveSuccessToResume = 4;


    // ״̬��
    protected int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;
    protected bool isInPauseForFailure = false;
    protected bool pausedByManager = false;   // ��PauseManager��ͣ
    protected KeyCode lastFailedKey;
    protected int consecutiveSuccessOnFailedKey = 0;

    // ����ʱ
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

    // ========== �������� ==========
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

    // ========== IPausableʵ�� ==========
    public void SetPaused(bool paused)
    {
        pausedByManager = paused;
        // ���趯��ͬ�����ᣬ�������ﴦ�� allAnimators
        if (paused)
            PauseAllAnimations();
        else
            ResumeAllAnimations();
    }

    // ========== ��ѭ�� ==========
    protected virtual void Update()
    {
        if (pausedByManager) return;         // ��ȫ�ֻ�ֲ�����
        if (isGameEnded) return;

        if (isInPauseForFailure)
        {
            HandlePauseRecoveryInput();
            return;
        }

        CheckBeatTiming();
        HandlePlayerInput();
    }

    // ========== ������� ==========
    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded || isInPauseForFailure) return;

        if (Input.GetKeyDown(keyConfig.primaryKey))
            OnKeyPressed(keyConfig.primaryKey);
        else if (Input.GetKeyDown(keyConfig.secondaryKey))
            OnKeyPressed(keyConfig.secondaryKey);
    }

    protected virtual void HandlePauseRecoveryInput()
    {
        if (Input.GetKeyDown(lastFailedKey))
        {
            consecutiveSuccessOnFailedKey++;
            ShowFeedback(lastFailedKey, successKeyColor);
            Debug.Log($"��ͣ�ָ�����: {consecutiveSuccessOnFailedKey}/{needConsecutiveSuccessToResume}");

            if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
                ResumeFromPause();
        }
        else if (IsOtherControllerKeyPressed())
        {
            KeyCode otherKey = (lastFailedKey == keyConfig.primaryKey) ? keyConfig.secondaryKey : keyConfig.primaryKey;
            if (Input.GetKeyDown(otherKey))
            {
                consecutiveSuccessOnFailedKey = 0;
                ShowFeedback(otherKey, missKeyColor);
                Debug.Log("����������ûָ�����");
            }
        }
    }

    protected bool IsOtherControllerKeyPressed()
    {
        KeyCode otherKey = (lastFailedKey == keyConfig.primaryKey) ? keyConfig.secondaryKey : keyConfig.primaryKey;
        return Input.GetKeyDown(otherKey);
    }

    // ========== �����߼� ==========
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

        EnterPauseForFailure();

        // ===== ֻ���⵱ǰ���������� =====
       

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
        }
    }

    protected virtual void EnterPauseForFailure()
    {
        isInPauseForFailure = true;
        lastFailedKey = expectedKey;
        consecutiveSuccessOnFailedKey = 0;

        if (beatCoroutine != null)
        {
            StopCoroutine(beatCoroutine);
            beatCoroutine = null;
        }

        PauseAllAnimations();
        SetKeyColor(lastFailedKey, missKeyColor);
        Debug.Log($"[{keyConfigPrefix}] ������ͣ״̬����Ҫ������ {lastFailedKey} �� {needConsecutiveSuccessToResume} �λָ�");
    }

    protected virtual void ResumeFromPause()
    {
        isInPauseForFailure = false;
        consecutiveSuccessOnFailedKey = 0;
        ResumeAllAnimations();
        SetAllKeysColor(normalKeyColor);

        // ==== �ָ�ȫ�� ====

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
        Debug.Log($"[{keyConfigPrefix}] ͨ�سɹ���");
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}] ��Ϸʧ�ܣ�");
    }

    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetAllKeysColor(normalKeyColor);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop && !isInPauseForFailure) StartNextBeat();
    }

    // ========== �Ӿ�������� ==========
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
            yield return new WaitForSecondsRealtime(feedbackDisplayDuration);
        }

        if (isInPauseForFailure && renderer == GetKeyRenderer(lastFailedKey))
            renderer.color = missKeyColor;
        else
            renderer.color = normalKeyColor;
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

    public virtual void StartRhythm()
    {
        if (isGameEnded) return;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }
}
