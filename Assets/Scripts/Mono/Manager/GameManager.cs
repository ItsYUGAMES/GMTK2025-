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

    public static GameManager instance;
    private int currentLevel = 1; // 当前关卡，基于场景位置
    [Header("游戏模式")]
    public bool isSingleMode = false; // 是否为Single模式

// 设置Single模式
    public void SetSingleMode(bool enabled)
    {
        isSingleMode = enabled;
        Debug.Log($"游戏模式设置为: {(enabled ? "Single" : "HotSeat")}");
    }

// 获取当前游戏模式
    public bool IsSingleMode()
    {
        return isSingleMode;
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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

    // 检查是否为gameplay场景
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

    // 进入下一关
    public void NextLevel()
    {
        if (currentLevel < levelScenes.Count)
        {
            int nextLevel = currentLevel + 1;
            string nextSceneName = levelScenes[nextLevel - 1];
            Debug.Log($"进入第 {nextLevel} 关: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("已达到最高关卡！");
        }
    }

    // 获取当前关卡
    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    // 获取当前关卡对应的场景名
    public string GetCurrentLevelScene()
    {
        if (currentLevel > 0 && currentLevel <= levelScenes.Count)
        {
            return levelScenes[currentLevel - 1];
        }
        return "";
    }

    // 更新游戏开始方法
    public void GameStart_Single()
    {
        SetSingleMode(true);
    }

    public void GameStart_HotSeat()
    {
        SetSingleMode(false);
    }
}