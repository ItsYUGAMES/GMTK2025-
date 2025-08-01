using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ���I���Ŀ�����������F���I���εĻ��A����
/// </summary>
public abstract class SingleKeyRhythmBase : MonoBehaviour
{
    [Header("���I�O��")]
    public KeyCode gameKey = KeyCode.Space;
    public string keyConfigPrefix = "Player";

    [Header("Prefab�O��")]
    public GameObject keyPrefab;
    public Vector3 keySpawnPosition;

    [Header("ҕ�X�O��")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("�����O��")]
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f;
    public bool infiniteLoop = true;

    [Header("�P��/ʧ���O��")]
    public int successToPass = 10;
    public int failToLose = 5;
    public int needConsecutiveSuccessToResume = 4;

    // ��B׃��
    public int successCount = 0;
    protected int failCount = 0;
    protected bool isGameEnded = false;
    public bool isPaused = false;
    protected bool pausedByManager = false;   // ��PauseManager��ͣ
    protected int consecutiveSuccessOnFailedKey = 0;

    // �\�Еr׃��
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

    // ========== �����L�� ==========
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

    // ========== ��ͣ���� ==========
    public void SetPaused(bool paused)
    {
        pausedByManager = paused;
        if (paused)
            PauseAllAnimations();
        else
            ResumeAllAnimations();
    }

    // ========== ��ѭ�h ==========
    protected virtual void Update()
    {
        if (pausedByManager) return;
        if (isGameEnded) return;

        CheckBeatTiming();
        HandleKeyInput();

        // ��ͣ��B��Ҳ̎��֏�ݔ��
        if (isPaused)
        {
            HandlePauseRecoveryInput();
        }
    }

    // ========== ݔ��̎�� ==========
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

    // ========== ���Ŀ��� ==========
    protected virtual void StartNextBeat()
    {
        if (pausedByManager) return;  // ֻ��ֹ��������ͣ�����Sʧ����ͣ�^�m����

        Debug.Log("NextBeat");
        currentBeatStartTime = Time.time;
        waitingForInput = true;
        SetKeyColor(highlightKeyColor);
        beatCounter++;

        // �z��ͨ�P�l��
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

        // ����ڕ�ͣ��B�³ɹ������ӻ֏�Ӌ��
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey++;
            Debug.Log($"��ͣ�֏��M��: {consecutiveSuccessOnFailedKey}/{needConsecutiveSuccessToResume}");

            // ����_���֏�Ҫ�󣬻֏��[��
            if (consecutiveSuccessOnFailedKey >= needConsecutiveSuccessToResume)
            {
                ResumeFromPause();
                return; // �֏�����؆��f�̣������@�eֱ�ӷ���
            }
        }

        // �����؆��f��
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void OnBeatFailed()
    {
        if (isGameEnded) return;

        failCount++;
        waitingForInput = false;
        ShowFeedback(missKeyColor);

        // ����ڕ�ͣ��B��ʧ�������û֏�Ӌ��
        if (isPaused)
        {
            consecutiveSuccessOnFailedKey = 0;
            Debug.Log("��ͣ��B��ʧ�������û֏��M��");
        }
        else
        {
            // ֻ���ڷǕ�ͣ��B�²��M�땺ͣ
            EnterPauseForFailure();
        }

        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        // �؆��f��
        if (beatCoroutine != null) StopCoroutine(beatCoroutine);
        beatCoroutine = StartCoroutine(WaitForNextBeat());
    }

    protected virtual void EnterPauseForFailure()
    {
        isPaused = true;
        consecutiveSuccessOnFailedKey = 0;

        // ����ͣ�Ӯ���׌һ�������\��
        // PauseAllAnimations();

        SetKeyColor(missKeyColor);
        Debug.Log($"[{keyConfigPrefix}] �M�땺ͣ��B����Ҫ�B�� {gameKey} �I {needConsecutiveSuccessToResume} �λ֏�");
    }

    protected virtual void ResumeFromPause()
    {
        isPaused = false;
        consecutiveSuccessOnFailedKey = 0;

        // ����Ҫ�֏̈́Ӯ������]�Е�ͣ
        // ResumeAllAnimations();

        SetKeyColor(normalKeyColor);

        // �����ąf���؆�߉݋���ֲ�׃
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
        Debug.Log($"[{keyConfigPrefix}] ͨ�P�ɹ���");
        OnLevelPassed?.Invoke();
    }

    protected virtual void OnGameFail()
    {
        Debug.Log($"[{keyConfigPrefix}] �[��ʧ����");
    }

    protected virtual IEnumerator WaitForNextBeat()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetKeyColor(normalKeyColor);
        yield return new WaitForSecondsRealtime(beatInterval - 0.1f);
        if (infiniteLoop) StartNextBeat();
    }

    // ========== ҕ�X����ϵ�y ==========
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

        // �޸��ɫ�֏�߉݋
        if (waitingForInput)
        {
            // ��ǰ���ĵ��Iʼ�K�@ʾ����
            renderer.color = highlightKeyColor;
        }
        else if (isPaused)
        {
            // ��ͣ��B�@ʾ�tɫ
            renderer.color = missKeyColor;
        }
        else
        {
            // ������r�@ʾ�����ɫ
            renderer.color = normalKeyColor;
        }
    }

    protected void SetKeyColor(Color color)
    {
        if (keySpriteRenderer != null)
        {
            if (keyColorCoroutine != null)
                StopCoroutine(keyColorCoroutine);

            // ��ͣ��B�µ�����̎��
            if (isPaused && color == normalKeyColor)
            {
                // �����Ҫ�O�Þ������ɫ���z���Ƿ�ԓ����
                if (waitingForInput)
                    keySpriteRenderer.color = highlightKeyColor;  // ��ǰ���ı��ָ���
                else
                    keySpriteRenderer.color = missKeyColor;       // ��ͣ��B���ּtɫ
            }
            else
            {
                keySpriteRenderer.color = color;
            }
        }
    }

    // ========== �������� ==========
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

    // ========== ��B��ԃ ==========
    public bool IsGameEnded() => isGameEnded;
    public bool IsGamePaused() => isPaused || pausedByManager;
    public int GetSuccessCount() => successCount;
    public int GetFailCount() => failCount;
    public int GetBeatCounter() => beatCounter;
}