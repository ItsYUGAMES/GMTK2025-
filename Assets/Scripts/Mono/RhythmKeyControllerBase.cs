using UnityEngine;
using System;
using System.Collections;

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

    [Header("�ؿ�/�������")]
    public int successToPass = 10;
    public int failToLose = 5;

    protected int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;

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

    // ��ͣ��������
    protected bool isInPauseForFailure = false;
    protected int consecutiveSuccessCount = 0;
    protected KeyCode nextExpectedKeyForRecovery;
    public int needConsecutiveSuccessToResume = 4;

    // ��̬��������¼�ĸ���������������ͣ
    protected static RhythmKeyControllerBase pauseOwner = null;

    // �������
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

        // �ռ����������ж�����
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

        // �����Ϸ��ͣ��
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            // ֻ�д�����ͣ�Ŀ��������ܴ���ָ�����
            if (pauseOwner == this && isInPauseForFailure)
            {
                HandlePauseRecoveryInput();
            }
            // ����������ʲô������
            return;
        }

        CheckBeatTiming();
        HandlePlayerInput();
    }

    protected virtual void HandlePlayerInput()
    {
        if (isGameEnded || isInPauseForFailure) return;

        // ֻ���������������õİ���
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
        // ȷ��ֻ����ͣ�������߲��ܴ�������
        if (pauseOwner != this) return;

        // ����ͣ�ָ��׶Σ���Ҫ����A-D-A-D����������
        if (Input.GetKeyDown(nextExpectedKeyForRecovery))
        {
            consecutiveSuccessCount++;
            ShowFeedback(nextExpectedKeyForRecovery, successKeyColor);

            // �л�����һ���ڴ��ļ�
            nextExpectedKeyForRecovery = (nextExpectedKeyForRecovery == keyConfig.primaryKey)
                ? keyConfig.secondaryKey : keyConfig.primaryKey;

            // ������һ����Ҫ���ļ�
            SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

            Debug.Log($"[{keyConfigPrefix}] ��ͣ�ָ�����: {consecutiveSuccessCount}/{needConsecutiveSuccessToResume}");

            if (consecutiveSuccessCount >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
            }
        }
        else if (Input.GetKeyDown(keyConfig.primaryKey) || Input.GetKeyDown(keyConfig.secondaryKey))
        {
            // ������˳������
            consecutiveSuccessCount = 0;
            ShowFeedback(Input.GetKeyDown(keyConfig.primaryKey) ? keyConfig.primaryKey : keyConfig.secondaryKey, missKeyColor);

            // ���´ӵ�һ������ʼ
            nextExpectedKeyForRecovery = keyConfig.primaryKey;
            SetAllKeysColor(normalKeyColor);
            SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

            Debug.Log($"[{keyConfigPrefix}] ����˳�����ûָ�����");
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
            // ��ʹ���ڵȴ����봰�ڣ�Ҳֻ�����õİ�����ʾ����
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
            // ֻ�а��˱��������ļ��������˲���ʧ��
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

        // ������ͣ״̬
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

        // �������������Ϊ��ͣ��������
        pauseOwner = this;

        // ���ûָ����д�������ʼ
        nextExpectedKeyForRecovery = keyConfig.primaryKey;

        // ֹͣ����Э��
        if (beatCoroutine != null)
        {
            StopCoroutine(beatCoroutine);
            beatCoroutine = null;
        }

        // ͨ��PauseManager��ͣ������Ϸ
        if (PauseManager.Instance != null)
            PauseManager.Instance.SetPause(true);

        // ��ͣ���ж���
        PauseAllAnimations();

        // ��ʾ��һ����Ҫ���ļ�
        SetAllKeysColor(normalKeyColor);
        SetKeyColor(nextExpectedKeyForRecovery, highlightKeyColor);

        Debug.Log($"[{keyConfigPrefix}] ������ͣ״̬����Ҫ��˳������ {keyConfig.primaryKey}-{keyConfig.secondaryKey} �� {needConsecutiveSuccessToResume} �λָ�");
    }

    protected virtual void ResumeFromPause()
    {
        isInPauseForFailure = false;
        consecutiveSuccessCount = 0;

        // �����ͣ������
        pauseOwner = null;

        // �ָ����ж���
        ResumeAllAnimations();

        // ͨ��PauseManager�ָ���Ϸ
        if (PauseManager.Instance != null)
            PauseManager.Instance.SetPause(false);

        // ���ð�����ɫ
        SetAllKeysColor(normalKeyColor);

        Debug.Log($"[{keyConfigPrefix}] �ָ���Ϸ");

        // ���¿�ʼ����
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void PauseAllAnimations()
    {
        // ��ͣ���ж�����
        for (int i = 0; i < allAnimators.Length; i++)
        {
            if (allAnimators[i] != null)
                allAnimators[i].speed = 0;
        }
    }

    protected virtual void ResumeAllAnimations()
    {
        // �ָ����ж�����
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

        // ����ͣ״̬��ʹ����ʵʱ��
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

        // �ָ���ɫ�߼�
        if (isInPauseForFailure && renderer == GetKeyRenderer(nextExpectedKeyForRecovery))
        {
            // �������һ���ڴ��ļ������ָ���
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
        // ȷ���ָ�ʱ������
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused && isInPauseForFailure)
        {
            PauseManager.Instance.SetPause(false);
            pauseOwner = null;
        }
    }
}