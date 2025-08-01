using UnityEngine;
using System.Collections;

public abstract class RhythmKeyControllerBase : MonoBehaviour
{
    [Header("��λ����")]
    public KeyConfig keyConfig = new KeyConfig();
    public string keyConfigPrefix = "Player"; // ����PlayerPrefs����

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

    [Header("����������")]
    public float slowMotionDuration = 1.5f;
    public float slowMotionTimeScale = 0.3f;
    public Color slowMotionTintColor = Color.blue;
    public GameObject slowMotionIndicator;

    [Header("�ؿ�/�������")]
    public int successToPass = 10;   // ͨ������ɹ�����
    public int failToLose = 5;       // ʧ�ܼ��ж�Ϊ��Ϸʧ��

    protected int successCount = 0;  // ��ǰ�ɹ�����
    protected int failCount = 0;     // ��ǰʧ�ܴ���
    protected bool isGameEnded = false; // �Ƿ��ѽ��

    // ����ʱ
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
        if (isGameEnded) return; // ��ֺ���Ӧ����

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

        // ����Ƿ���ͨ������
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

        // ����Ƿ�ʧ��
        if (failCount >= failToLose)
        {
            isGameEnded = true;
            OnGameFail();
            return;
        }

        StartCoroutine(WaitForNextBeat());
    }

    // �������Ҳ��ʧ�ܣ�ֱ�Ӹ���
    protected virtual void OnBeatMissed()
    {
        OnBeatFailed();
    }
    // �ɹ�ͨ�ؽ��
    protected virtual void OnGameSuccess()
    {
        Debug.Log(" ͨ�سɹ������ڴ˼�����һ�ء���ʾͨ�ؽ��桢��������ȡ�");
        // ���磺SceneManager.LoadScene("NextLevel");
    }

    // ��Ϸʧ�ܽ��
    protected virtual void OnGameFail()
    {
        Debug.Log(" ��Ϸʧ�ܣ����ڴ���ʾʧ�ܽ��桢���԰�ť�ȡ�");
        // ���磺SceneManager.LoadScene("GameOver");
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

    // ������
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

    // ---- ��ѡ����¶���ü�λ���� ----
    public void ResetToDefaultKey()
    {
        keyConfig = new KeyConfig();
        keyConfig.Save(keyConfigPrefix);
    }

    // ����Ľ�β���ϣ�
    protected bool isReadyToStart = false;

    public virtual void StartRhythm()
    {
        if (isReadyToStart) return; // ��ֹ�ظ�����
        isReadyToStart = true;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
    }

}
