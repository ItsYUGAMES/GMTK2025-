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
    
    [Header("游戏模式")]
    public bool isSingleKeyMode = false;

    
    [Header("Prefab配置")]
    public GameObject primaryKeyPrefab;  // 主键prefab
    public GameObject secondaryKeyPrefab; // 副键prefab
    public Vector3 primaryKeyPosition = Vector3.zero;  // 主键实例化坐标
    public Vector3 secondaryKeyPosition = Vector3.zero; // 副键实例化坐标
    [Header("视觉反馈")]
    private SpriteRenderer primaryKeyRenderer;
    private SpriteRenderer secondaryKeyRenderer;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color successColor = Color.green;

    private int currentSuccessCount = 0;
    private bool isWaitingForInput = false;
    private bool isCompleted = false;
    private KeyCode currentExpectedKey;
    private List<MonoBehaviour> pausedBehaviors = new List<MonoBehaviour>();
    private GameObject instantiatedPrimaryKey;
    private GameObject instantiatedSecondaryKey;
    void Start()
    {
        // 根据GameManager获取游戏模式
        if (GameManager.Instance != null)
        {
            isSingleKeyMode = GameManager.Instance.IsSingleMode();
        }

        InstantiateKeyPrefabs();
    
        PauseAllOtherScripts();
        StartCoroutine(StartSequence());
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
            // if (!mb.enabled)
            //     continue;
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
        // 设置所有按键为正常颜色
        SetAllKeysColor(normalColor);

        if (isSingleKeyMode)
        {
            // 单键模式：只高亮主键
            SetKeyColor(primaryKey, highlightColor);
        }
        else
        {
            // 双键模式：高亮当前期望的按键
            SetKeyColor(currentExpectedKey, highlightColor);
        }

        isWaitingForInput = true;

        // 等待玩家输入或超时
        float timeLeft = beatInterval;
        while (timeLeft > 0 && isWaitingForInput)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // 如果超时未按键，重置计数
        if (isWaitingForInput)
        {
            OnBeatMissed();
        }

        isWaitingForInput = false;

        // 如果按键成功，显示绿色反馈
        if (currentSuccessCount > 0)
        {
            if (isSingleKeyMode)
            {
                // 单键模式：只在主键上显示成功反馈
                SetKeyColor(primaryKey, successColor);
            }
            else
            {
                // 双键模式：在期望键上显示成功反馈
                SetKeyColor(currentExpectedKey, successColor);
            }
            yield return new WaitForSeconds(0.1f);
        }

        // 恢复按键颜色
        SetAllKeysColor(normalColor);
        yield return new WaitForSeconds(0.2f);
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
    }

    private void SetKeyColor(KeyCode key, Color color)
    {
        if (key == primaryKey && primaryKeyRenderer != null)
            primaryKeyRenderer.color = color;
        else if (key == secondaryKey && secondaryKeyRenderer != null)
            secondaryKeyRenderer.color = color;
    }

    private void SetAllKeysColor(Color color)
    {
        if (primaryKeyRenderer != null)
            primaryKeyRenderer.color = color;
        if (secondaryKeyRenderer != null)
            secondaryKeyRenderer.color = color;
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
        
        
    }
}