using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager Instance { get; private set; }

    private bool isGamePaused = false;
    private bool isInRecoveryMode = false;
    private List<MonoBehaviour> pausedScripts = new List<MonoBehaviour>();
    
    [Header("恢复判定设置")]
    public int requiredSuccessfulInputs = 3; // 需要连续成功的按键次数
    private int currentSuccessfulInputs = 0;
    private KeyCode targetKey; // 需要判定的按键
    
    // 不需要暂停的脚本类型
    private readonly System.Type[] excludedTypes = {
        typeof(RhythmKeyControllerBase),
        typeof(JLController),
        typeof(GamePauseManager),
        // 可以添加其他按键判定相关脚本
    };

    // 事件：恢复判定开始和结束
    public System.Action<KeyCode, int> OnRecoveryModeStarted;
    public System.Action OnRecoveryModeCompleted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 处理恢复模式的按键检测
        if (isInRecoveryMode)
        {
            HandleRecoveryInput();
        }
    }

    public void PauseGameForRecovery(KeyCode missedKey)
    {
        if (isGamePaused) return;

        isGamePaused = true;
        isInRecoveryMode = true;
        targetKey = missedKey;
        currentSuccessfulInputs = 0;
        pausedScripts.Clear();

        // 获取场景中所有的 MonoBehaviour
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            // 跳过自己和排除的脚本类型
            if (script == this || IsExcludedType(script.GetType()))
                continue;

            // 如果脚本当前是启用的，暂停它
            if (script.enabled)
            {
                script.enabled = false;
                pausedScripts.Add(script);
            }
        }

        // 不完全暂停时间，保持一定的时间流动以支持按键检测
        Time.timeScale = 0.1f;

        Debug.Log($"游戏已暂停，开始恢复判定模式 - 目标按键: {targetKey}，需要连续成功: {requiredSuccessfulInputs} 次");
        
        // 触发恢复模式开始事件
        OnRecoveryModeStarted?.Invoke(targetKey, requiredSuccessfulInputs);
    }

    private void HandleRecoveryInput()
    {
        if (Input.GetKeyDown(targetKey))
        {
            currentSuccessfulInputs++;
            Debug.Log($"恢复判定成功！当前进度: {currentSuccessfulInputs}/{requiredSuccessfulInputs}");
            
            // 检查是否达到要求的成功次数
            if (currentSuccessfulInputs >= requiredSuccessfulInputs)
            {
                CompleteRecovery();
            }
        }
        
        // 检测错误按键
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key) && key != targetKey && IsGameKey(key))
            {
                // 按错键重置进度
                currentSuccessfulInputs = 0;
                Debug.Log($"按错键！恢复判定重置，目标按键: {targetKey}");
                break;
            }
        }
    }

    private void CompleteRecovery()
    {
        isInRecoveryMode = false;
        Debug.Log("恢复判定完成！游戏即将恢复");
        
        // 触发恢复完成事件
        OnRecoveryModeCompleted?.Invoke();
        
        // 延迟一点时间再恢复，让玩家有反应时间
        StartCoroutine(DelayedResume());
    }

    private IEnumerator DelayedResume()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ResumeGame();
    }

    public void ResumeGame()
    {
        if (!isGamePaused) return;

        isGamePaused = false;
        isInRecoveryMode = false;
        currentSuccessfulInputs = 0;

        // 恢复所有被暂停的脚本
        foreach (MonoBehaviour script in pausedScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        pausedScripts.Clear();

        // 恢复时间缩放
        Time.timeScale = 1f;

        Debug.Log("游戏已恢复");
    }

    private bool IsExcludedType(System.Type scriptType)
    {
        foreach (System.Type excludedType in excludedTypes)
        {
            if (excludedType.IsAssignableFrom(scriptType))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsGameKey(KeyCode key)
    {
        // 定义游戏中使用的按键，避免检测系统按键
        return key == KeyCode.A || key == KeyCode.S || key == KeyCode.D || key == KeyCode.F ||
               key == KeyCode.J || key == KeyCode.K || key == KeyCode.L || key == KeyCode.Semicolon ||
               key == KeyCode.LeftArrow || key == KeyCode.RightArrow || key == KeyCode.UpArrow || key == KeyCode.DownArrow;
    }

    // 公共属性和方法
    public bool IsGamePaused() => isGamePaused;
    public bool IsInRecoveryMode() => isInRecoveryMode;
    public int GetCurrentRecoveryProgress() => currentSuccessfulInputs;
    public int GetRequiredInputs() => requiredSuccessfulInputs;
    public KeyCode GetTargetKey() => targetKey;
}