using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("关卡设置")]
    public List<string> levelScenes = new List<string>(); // 关卡场景名列表

    [Header("进度条设置")]
    public ProgressBarController progressBar; // 进度条引用

    [Header("失败管理")]
    public FailManager failManager;
    public static GameManager Instance { get; private set; }
    private int currentLevel = 1; // 当前关卡，基于场景位置
    [Header("游戏模式")]
    public bool isSingleMode = false; // 是否为Single模式

// 设置Single模式
    public void SetSingleMode(bool enabled)
    {
        isSingleMode = enabled;
        Debug.Log($"游戏模式设置为: {(enabled ? "Single" : "HotSeat")}");
    }
    
    [ContextMenu("切换到Intro场景")]
    public void LoadIntroScene()
    {
        Debug.Log("切换到Intro场景");
        SceneManager.LoadScene("Intro"); // 直接调用SceneManager，避免递归
    }

// 获取当前游戏模式
    public bool IsSingleMode()
    {
        return isSingleMode;
    }
    void Awake()
    {
        // 单例模式 - 确保只有一个GameManager实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }

    void Start()
    {
        CheckGameplayScene();
       
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckGameplayScene();
    
        // 控制IntroManager的启用状态
        IntroManager introManager = GetComponent<IntroManager>();
        failManager = GetComponent<FailManager>();
        if (introManager != null)
        {
            if (scene.name == "Intro")
            {
                introManager.enabled = true;
                Debug.Log("IntroManager已启用");
            }
            else
            {
                introManager.enabled = false;
                Debug.Log("IntroManager已禁用");
            }
        }

        if (failManager!=null)
        {
            if (scene.name=="Fail")
            {
                failManager.enabled = true;
            }
            else
            {
                failManager.enabled = false;
            }
        }
    }

    // 根据当前场景名更新关卡
    private void UpdateLevelFromCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        int sceneIndex = levelScenes.IndexOf(currentSceneName);
    
        if (sceneIndex >= 0)
        {
            currentLevel = sceneIndex + 1; // 直接设置内存中的关卡号
            Debug.Log($"从场景列表获取关卡: {currentSceneName}, 关卡号: {currentLevel}");
        }
        else
        {
            Debug.LogWarning($"场景 {currentSceneName} 不在关卡列表中");
        }
    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

  
    public bool CheckGameplayScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name.ToLower();
    
        // 更新关卡信息
        UpdateLevelFromCurrentScene();
    
        if (currentSceneName == "gameplay")
        {
            return true;
        }
    
        return false;
    }
    

    // 更新游戏开始方法
    public void GameStart_Single()
    {
        SetSingleMode(true);
        LoadScene("Intro");
    }

    public void GameStart_HotSeat()
    {
        SetSingleMode(false);
        LoadScene("Intro");
    }

    
}