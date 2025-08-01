using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerGoldUI : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI goldText;
    public Text goldTextLegacy;
    public string goldPrefix = "金币: ";
    public string goldSuffix = "";

    [Header("动画设置")]
    public bool enableCountAnimation = true;
    public float animationDuration = 0.5f;

    private int currentDisplayGold = 0;
    private int targetGold = 0;
    private bool isSubscribed = false;

    void Awake()
    {
        // 在Awake中尝试立即初始化
        TryInitialize();
    }

    void Start()
    {
        // 在Start中再次尝试初始化
        TryInitialize();
    }

    void Update()
    {
        // 如果还没有订阅成功，继续尝试
        if (!isSubscribed)
        {
            TryInitialize();
        }
    }

    private void TryInitialize()
    {
        if (PlayerDataManager.Instance != null && !isSubscribed)
        {
            // 订阅事件
            PlayerDataManager.Instance.OnGoldChanged += OnGoldChanged;
            isSubscribed = true;
            
            // 立即获取当前金币并显示
            int currentGold = PlayerDataManager.Instance.GetPlayerGold();
            currentDisplayGold = currentGold;
            targetGold = currentGold;
            
            // 强制更新显示
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
        else if (goldTextLegacy != null)
        {
            goldTextLegacy.text = displayText;
            Debug.Log($"Text 显示更新为: {displayText}");
        }
        else
        {
            Debug.LogError("goldText 和 goldTextLegacy 都为空！请在Inspector中分配UI组件。");
        }
    }

    void OnDestroy()
    {
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
}