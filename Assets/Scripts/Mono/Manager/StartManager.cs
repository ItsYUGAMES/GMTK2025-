using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartManager : MonoBehaviour
{
    [Header("按键配置")]
    public KeyCode primaryKey = KeyCode.A;
    public KeyCode secondaryKey = KeyCode.D;

    [Header("判定设置")]
    public int requiredSuccessCount = 4;
    public float beatInterval = 1.0f;
    public float successWindow = 0.4f; // 判定窗口期（秒）

    [Header("游戏模式")]
    public bool isSingleKeyMode = false;

    [Header("Prefab配置")]
    public GameObject primaryKeyPrefab;  // 主键prefab
    public GameObject secondaryKeyPrefab; // 副键prefab
    public Vector3 primaryKeyPosition = Vector3.zero;  // 主键实例化坐标
    public Vector3 secondaryKeyPosition = Vector3.zero; // 副键实例化坐标

    [Header("Sprite设置")]
    public Sprite normalKeySprite;
    public Sprite highlightKeySprite;
    public Sprite successKeySprite;
    public Sprite missKeySprite;  // 添加失败sprite
    public float feedbackDisplayDuration = 0.2f;
    
    [Header("音效设置")]
    public AudioClip moveUpDownSFX; // Up和Down移动音效
    
    private SpriteRenderer primaryKeyRenderer;
    private SpriteRenderer secondaryKeyRenderer;
    private int currentSuccessCount = 0;
    private bool isWaitingForInput = false;
    private bool isCompleted = false;
    private KeyCode currentExpectedKey;
    private List<MonoBehaviour> pausedBehaviors = new List<MonoBehaviour>();
    private GameObject instantiatedPrimaryKey;
    private GameObject instantiatedSecondaryKey;
    private bool isMoving = false; // 添加移动状态跟踪
    private LongPressController longPressController;
    void Start()
    {
        // 根据GameManager获取游戏模式
        if (GameManager.Instance != null)
        {
            isSingleKeyMode = GameManager.Instance.IsSingleMode();
        }

        // 先移动Up和Down对象
        StartCoroutine(MoveUpDownObjects());

        InstantiateKeyPrefabs();
        PauseAllOtherScripts();

        // 检查是否有LongPressController，如果有则等待其完成
        longPressController = FindObjectOfType<LongPressController>();
        if (longPressController != null)
        {
            Debug.Log("检测到LongPressController，等待第一次长按成功");
            StartCoroutine(WaitForLongPressAndStart(longPressController));
        }
        
        // 如果没有LongPressController，StartSequence将在MoveUpDownObjects完成后启动
    }

    private IEnumerator MoveUpDownObjects()
    {
        isMoving = true; // 开始移动
        
        // 查找Up和Down对象
        GameObject upObject = GameObject.Find("Up");
        GameObject downObject = GameObject.Find("Down");
    
        if (upObject == null || downObject == null)
        {
            Debug.LogWarning("未找到Up或Down对象");
            isMoving = false;
            
            // 如果没有LongPressController，直接开始序列
           
            if (this.longPressController == null)
            {
                StartCoroutine(StartSequence());
            }
            yield break;
        }

        Vector3 upStartPos = upObject.transform.position;
        Vector3 downStartPos = downObject.transform.position;
        Vector3 upTargetPos = upStartPos + Vector3.up * 9f;
        Vector3 downTargetPos = downStartPos + Vector3.down * 9f;

        float moveDuration = 1.0f; // 移动持续时间
        float elapsedTime = 0f;

        Debug.Log("开始移动Up和Down对象");
        // 播放移动音效
        if (SFXManager.Instance != null && moveUpDownSFX != null)
        {
            AudioSource AudioSource = SFXManager.Instance.GetAudioSource();
            AudioSource.clip = moveUpDownSFX;
            AudioSource.time = 0.3f; // 从0.3秒开始播放
            SFXManager.Instance.PlaySFX(moveUpDownSFX);
            Debug.Log("播放Up和Down移动音效");
        }
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float journey = elapsedTime / moveDuration;

            upObject.transform.position = Vector3.Lerp(upStartPos, upTargetPos, journey);
            downObject.transform.position = Vector3.Lerp(downStartPos, downTargetPos, journey);

            yield return null;
        }

        // 确保最终位置精确
        upObject.transform.position = upTargetPos;
        downObject.transform.position = downTargetPos;

        Debug.Log("Up和Down对象移动完成");
        isMoving = false; // 移动完成

        // 如果没有LongPressController，直接开始序列
        LongPressController longPressController = FindObjectOfType<LongPressController>();
        if (longPressController == null)
        {
            StartCoroutine(StartSequence());
        }
    }

    private IEnumerator WaitForLongPressAndStart(LongPressController longPressController)
    {
        // 等待移动完成
        yield return new WaitUntil(() => !isMoving);

        Debug.Log("请进行第一次长按以开始节奏序列");

        // 等待长按成功
        yield return new WaitUntil(() => longPressController.successCount >= 1);

        Debug.Log("第一次长按成功，开始序列");

        // 直接完成序列，恢复脚本并禁用StartManager
        OnSequenceCompleted();
    }

    void Update()
    {
        if (isCompleted || !isWaitingForInput) return;

        if (isSingleKeyMode)
        {
            // 单键模式：只检测A键
            if (Input.GetKeyDown(primaryKey))
            {
                OnKeyPressed();
            }
        }
        else
        {
            // 双键模式：检测期望的键
            if (Input.GetKeyDown(currentExpectedKey))
            {
                OnKeyPressed();
            }
        }
    }

    private void InstantiateKeyPrefabs()
    {
        Quaternion rotation = Quaternion.identity;

        if (isSingleKeyMode)
        {
            // 单键模式：只实例化主键
            if (primaryKeyPrefab != null)
            {
                instantiatedPrimaryKey = Instantiate(primaryKeyPrefab, primaryKeyPosition, rotation);
                primaryKeyRenderer = instantiatedPrimaryKey.GetComponent<SpriteRenderer>();
            }
        }
        else
        {
            // 双键模式：实例化两个按键
            if (primaryKeyPrefab != null)
            {
                instantiatedPrimaryKey = Instantiate(primaryKeyPrefab, primaryKeyPosition, rotation);
                primaryKeyRenderer = instantiatedPrimaryKey.GetComponent<SpriteRenderer>();
            }

            if (secondaryKeyPrefab != null)
            {
                instantiatedSecondaryKey = Instantiate(secondaryKeyPrefab, secondaryKeyPosition, rotation);
                secondaryKeyRenderer = instantiatedSecondaryKey.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void PauseAllOtherScripts()
    {
        pausedBehaviors.Clear();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == this || mb is PauseManager)
                continue;
            if (mb == this || mb is SFXManager)
                continue;
            if (mb == this || mb is LongPressController)
                continue;
            // 检查是否挂载在 Canvas 对象上
            if (mb.GetComponentInParent<Canvas>() != null)
            {
                // 如果是 ProgressBarController，需要暂停
                if (mb.GetType().Name == "ProgressBarController")
                {
                    pausedBehaviors.Add(mb);
                    mb.enabled = false;
                    continue;
                }
                else
                {
                    continue; // 其他 Canvas 组件跳过
                }
            }
        
            pausedBehaviors.Add(mb);
            mb.enabled = false;
        }
        Debug.Log($"已暂停 {pausedBehaviors.Count} 个脚本");
    }

    private void ResumeAllOtherScripts()
    {
        foreach (var mb in pausedBehaviors)
            if (mb != null) mb.enabled = true;
        pausedBehaviors.Clear();
        Debug.Log("已恢复所有脚本");
    }

    private IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.5f);

        while (currentSuccessCount < requiredSuccessCount && !isCompleted)
        {
            // 交替设置期望按键
            currentExpectedKey = (currentSuccessCount % 2 == 0) ? primaryKey : secondaryKey;
            yield return StartCoroutine(WaitForBeat());
        }

        if (currentSuccessCount >= requiredSuccessCount)
        {
            OnSequenceCompleted();
        }
    }

    private IEnumerator WaitForBeat()
    {
        // 设置所有按键为正常sprite
        SetAllKeysSprite(normalKeySprite);

        if (isSingleKeyMode)
        {
            // 单键模式：只高亮主键
            SetKeySprite(primaryKey, highlightKeySprite);
        }
        else
        {
            // 双键模式：高亮当前期望的按键
            SetKeySprite(currentExpectedKey, highlightKeySprite);
        }

        float currentBeatStartTime = Time.time;
        isWaitingForInput = true;

        // 等待判定窗口期内的输入
        float timeLeft = successWindow;
        while (timeLeft > 0 && isWaitingForInput)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // 如果超时未按键，重置计数
        if (isWaitingForInput)
        {
            OnBeatMissed();
            // 等待失败反馈显示
            yield return new WaitForSeconds(feedbackDisplayDuration);
        }

        isWaitingForInput = false;

        // 显示成功反馈
        if (currentSuccessCount > 0)
        {
            if (isSingleKeyMode)
            {
                SetKeySprite(primaryKey, successKeySprite);
            }
            else
            {
                SetKeySprite(currentExpectedKey, successKeySprite);
            }
            yield return new WaitForSeconds(feedbackDisplayDuration);
        }

        // 恢复按键sprite
        SetAllKeysSprite(normalKeySprite);

        // 等待剩余时间到下一个节拍
        float remainingTime = beatInterval - successWindow - feedbackDisplayDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }
    }

    private void OnKeyPressed()
    {
        if (!isWaitingForInput) return;

        isWaitingForInput = false;
        currentSuccessCount++;

        string mode = isSingleKeyMode ? "Single" : "HotSeat";
        Debug.Log($"{mode}模式 - 成功判定 {currentSuccessCount}/{requiredSuccessCount}");
    }

    private void OnBeatMissed()
    {
        currentSuccessCount = 0;
        string mode = isSingleKeyMode ? "Single" : "Hotseat";
        Debug.Log($"{mode}模式 - 判定失败，重新开始");
    
        // 显示失败反馈
        if (isSingleKeyMode)
        {
            SetKeySprite(primaryKey, missKeySprite);
        }
        else
        {
            SetKeySprite(currentExpectedKey, missKeySprite);
        }
    }
    
    private void SetKeySprite(KeyCode key, Sprite sprite)
    {
        if (key == primaryKey && primaryKeyRenderer != null)
            primaryKeyRenderer.sprite = sprite;
        else if (key == secondaryKey && secondaryKeyRenderer != null)
            secondaryKeyRenderer.sprite = sprite;
    }

    private void SetAllKeysSprite(Sprite sprite)
    {
        if (primaryKeyRenderer != null)
            primaryKeyRenderer.sprite = sprite;
        if (secondaryKeyRenderer != null)
            secondaryKeyRenderer.sprite = sprite;
    }

    private void OnSequenceCompleted()
    {
        isCompleted = true;
        string mode = isSingleKeyMode ? "单键" : "双键";
        Debug.Log($"{mode}模式 - 开始序列完成，启动游戏");

        // 销毁实例化的prefab
        if (instantiatedPrimaryKey != null)
        {
            Destroy(instantiatedPrimaryKey);
            instantiatedPrimaryKey = null;
        }

        if (instantiatedSecondaryKey != null)
        {
            Destroy(instantiatedSecondaryKey);
            instantiatedSecondaryKey = null;
        }

        ResumeAllOtherScripts();

        var controllers = FindObjectsOfType<RhythmKeyControllerBase>();
        foreach (var controller in controllers)
        {
            controller.StartRhythm();
        }
        
        // 禁用此脚本，防止它在完成任务后继续运行
        this.enabled = false;
        Debug.Log("StartManager 脚本已禁用。");
    }
}