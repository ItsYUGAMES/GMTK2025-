using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerGoldUI : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI goldText;

    [Header("自动查找设置")]
    public string moneyTextName = "MoneyText"; // 要查找的GameObject名称
    public bool autoFindMoneyText = true; // 是否自动查找

    [Header("显示格式")]
    public string goldPrefix = ""; // 金币前缀
    public string goldSuffix = ""; // 金币后缀

    [Header("动画设置")]
    public bool enableCountAnimation = true;
    public float animationDuration = 0.5f;

    private int currentDisplayGold = 0;
    private int targetGold = 0;
    private bool isSubscribed = false;

    void Awake()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInitialize();
    }

    void Start()
    {
        TryInitialize();
    }

    void Update()
    {
        if (!isSubscribed)
        {
            TryInitialize();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成: {scene.name}，开始查找UI组件");

        // 场景切换后重新查找UI组件
        if (autoFindMoneyText)
        {
            FindMoneyTextInScene();
        }

        // 重新初始化
        TryInitialize();
    }

    private void FindMoneyTextInScene()
    {
        // 先尝试通过名称查找
        GameObject moneyTextObj = GameObject.Find(moneyTextName);

        if (moneyTextObj != null)
        {
            TextMeshProUGUI tmpComponent = moneyTextObj.GetComponent<TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                goldText = tmpComponent;
                Debug.Log($"找到TextMeshProUGUI组件: {moneyTextName}");
                return;
            }
        }

        // 如果通过名称找不到，尝试在所有UI组件中查找
        if (goldText == null)
        {
            FindMoneyTextByKeyword();
        }
    }

    private void FindMoneyTextByKeyword()
    {
        // 查找所有TextMeshProUGUI组件
        TextMeshProUGUI[] allTMPTexts = FindObjectsOfType<TextMeshProUGUI>();
        foreach (var tmp in allTMPTexts)
        {
            if (tmp.gameObject.name.Contains("Money") || tmp.gameObject.name.Contains("Gold") || tmp.gameObject.name.Contains("Coin"))
            {
                goldText = tmp;
                Debug.Log($"通过名称匹配找到TextMeshProUGUI: {tmp.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("未找到合适的金币显示UI组件");
    }

    private void TryInitialize()
    {
        if (PlayerDataManager.Instance != null && !isSubscribed)
        {
            PlayerDataManager.Instance.OnGoldChanged += OnGoldChanged;
            isSubscribed = true;

            int currentGold = PlayerDataManager.Instance.GetPlayerGold();
            currentDisplayGold = currentGold;
            targetGold = currentGold;

            ForceUpdateDisplay(currentGold);
            Debug.Log($"PlayerGoldUI 初始化完成，当前金币: {currentGold}");
        }
    }

    private void ForceUpdateDisplay(int gold)
    {
        string displayText = goldPrefix + gold.ToString() + goldSuffix;

        if (goldText != null)
        {
            goldText.text = displayText;
            Debug.Log($"TextMeshProUGUI 显示更新为: {displayText}");
        }
        else
        {
            Debug.LogError("goldText 为空！请检查场景中是否有正确的UI组件。");
        }
    }

    void OnDestroy()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (PlayerDataManager.Instance != null && isSubscribed)
        {
            PlayerDataManager.Instance.OnGoldChanged -= OnGoldChanged;
            isSubscribed = false;
        }
    }

    private void OnGoldChanged(int newGold)
    {
        Debug.Log($"PlayerGoldUI 收到金币变化事件: {newGold}");
        targetGold = newGold;

        if (enableCountAnimation && Application.isPlaying)
        {
            StartCoroutine(AnimateGoldCount());
        }
        else
        {
            UpdateGoldDisplay(targetGold, true);
        }
    }

    private System.Collections.IEnumerator AnimateGoldCount()
    {
        int startGold = currentDisplayGold;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

            int displayGold = Mathf.RoundToInt(Mathf.Lerp(startGold, targetGold, progress));
            UpdateGoldDisplay(displayGold, false);

            yield return null;
        }

        UpdateGoldDisplay(targetGold, true);
    }

    private void UpdateGoldDisplay(int gold, bool updateCurrent)
    {
        if (updateCurrent)
        {
            currentDisplayGold = gold;
        }

        ForceUpdateDisplay(gold);
    }

    [ContextMenu("手动刷新金币显示")]
    public void ManualRefresh()
    {
        if (PlayerDataManager.Instance != null)
        {
            ForceUpdateDisplay(PlayerDataManager.Instance.GetPlayerGold());
        }
    }

    [ContextMenu("测试增加金币")]
    public void TestAddGold()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddPlayerGold(100);
        }
    }

    [ContextMenu("重新查找MoneyText")]
    public void RefindMoneyText()
    {
        FindMoneyTextInScene();
        if (PlayerDataManager.Instance != null)
        {
            ForceUpdateDisplay(PlayerDataManager.Instance.GetPlayerGold());
        }
    }
}