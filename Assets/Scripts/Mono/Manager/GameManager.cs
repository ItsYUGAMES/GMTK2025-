
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
    
    [Header("音效设置")]
    public AudioClip transitionCompleteSFX; // Transition场景移动完成音效

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

        // 延迟播放背景音乐，确保SFXManager已初始化
       

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

        if (failManager != null)
        {
            if (scene.name == "Fail")
            {
                failManager.enabled = true;
            }
            else
            {
                failManager.enabled = false;
            }
        }
    }

    // 延迟播放关卡音乐的协程
    

    /// <summary>
    /// 切换到下一关卡
    /// </summary>
    ///
    [ContextMenu("加载下一关卡")]
    public void LoadNextLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName)
        {
            case "Level1":
                LoadScene("Level2");
                break;
            case "Level2":
                LoadScene("Level3");
                break;
            case "Level3":
                LoadScene("Level4");
                break;
            case "Level4":
                LoadScene("Level5");
                break;
            case "Level5":
                LoadScene("Level6");
                break;
            case "Level6":
                LoadScene("End");
                break;
            default:
                Debug.LogWarning($"未知场景: {currentSceneName}");
                break;
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

        if (currentSceneName != "Level2")
        {
            return true;
        }

        // 在 transition 场景中重置对象位置
        if (currentSceneName == "transition")
        {
            // 延迟执行，等待场景完全加载
            StartCoroutine(WaitAndMoveObjects());
        }

        return false;
    }

    // 等待一帧后再移动对象
    private System.Collections.IEnumerator WaitAndMoveObjects()
    {
        // 等待更长时间，确保所有对象都完全初始化
        yield return new WaitForSeconds(0.1f);

        GameObject upObj = GameObject.Find("Up");
        GameObject downObj = GameObject.Find("Down");

        if (upObj == null || downObj == null)
        {
            Debug.LogWarning("在Transition场景中未找到Up或Down对象");
            yield break;
        }

        // 检查对象是否已经在原点
        if (Vector3.Distance(upObj.transform.position, Vector3.zero) < 0.1f &&
            Vector3.Distance(downObj.transform.position, Vector3.zero) < 0.1f)
        {
            Debug.Log("Up和Down对象已经在原点附近，跳过移动动画");
            // 直接播放音效
            if (SFXManager.Instance != null && transitionCompleteSFX != null)
            {
                SFXManager.Instance.PlaySFX(transitionCompleteSFX);
            }
            yield break;
        }

        Debug.Log($"找到Up对象: {upObj.name}, 位置: {upObj.transform.position}");
        Debug.Log($"找到Down对象: {downObj.name}, 位置: {downObj.transform.position}");

        StartCoroutine(MoveObjectsToOrigin(upObj, downObj));
    }

    // 重置 Up 和 Down 对象到原点
    private void ResetUpDownObjectsToOrigin()
    {
        GameObject upObj = GameObject.Find("Up");
        GameObject downObj = GameObject.Find("Down");

        if (upObj != null || downObj != null)
        {
            StartCoroutine(MoveObjectsToOrigin(upObj, downObj));
        }
    }

    private System.Collections.IEnumerator MoveObjectsToOrigin(GameObject upObj, GameObject downObj)
    {
        float moveSpeed = 3f; // 降低移动速度，让动画更明显
        Vector3 targetPos = Vector3.zero;

        Vector3 upStartPos = upObj.transform.position;
        Vector3 downStartPos = downObj.transform.position;

        Debug.Log($"开始移动 - Up起始位置: {upStartPos}, Down起始位置: {downStartPos}, 目标位置: {targetPos}");

        // 确保起始位置不在原点
        if (Vector3.Distance(upStartPos, targetPos) < 0.1f && Vector3.Distance(downStartPos, targetPos) < 0.1f)
        {
            Debug.Log("对象已经在原点，无需移动");
            yield break;
        }

        float journey = 0f;

        // 移动动画
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;

            upObj.transform.position = Vector3.Lerp(upStartPos, targetPos, journey);
            downObj.transform.position = Vector3.Lerp(downStartPos, targetPos, journey);

            yield return null;
        }

        // 确保最终位置精确
        upObj.transform.position = targetPos;
        downObj.transform.position = targetPos;

        Debug.Log("Up和Down对象已移动到原点");

        // 移动完成后播放音效，从0.3秒开始
        if (SFXManager.Instance != null && transitionCompleteSFX != null)
        {
            AudioSource audioSource = SFXManager.Instance.GetAudioSource();
            audioSource.clip = transitionCompleteSFX;
            audioSource.time = 0.4f; // 从0.3秒开始播放
            SFXManager.Instance.PlaySFX(transitionCompleteSFX);
            Debug.Log("播放移动完成音效（从0.4秒开始）");
        }
    }

    // 获取移动完成音效的方法，你需要在GameManager中添加相应的AudioClip字段
    private AudioClip GetMoveCompleteAudioClip()
    {
        return transitionCompleteSFX;
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
