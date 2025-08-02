using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("玩家数据")]
    [SerializeField] private int playerGold = 0;
 
    [Header("道具效果状态")]
    public bool cheerBoostActive = false;     // 欢呼加速效果
    public bool extraRewardActive = false;
    public bool extraLifeActive = false;
    public bool autoPlayActive = false;
    public bool holdModeActive = false;     // 添加长按模式状态

    // 金币变化事件
    public System.Action<int> OnGoldChanged;

    void Awake()
    {
        // 单例模式
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
        ReapplyAllActiveItems();
    }
    #region 金币管理
    public int GetPlayerGold()
    {
        return playerGold;
    }

    public void AddPlayerGold(int amount)
    {
        if (amount <= 0) return;

        playerGold += amount;
        OnGoldChanged?.Invoke(playerGold);
        Debug.Log($"玩家获得 {amount} 金币，当前总金币: {playerGold}");
    }

    public bool DeductPlayerGold(int amount)
    {
        if (amount <= 0 || playerGold < amount)
        {
            Debug.LogWarning($"金币不足，当前金币: {playerGold}，尝试扣除: {amount}");
            return false;
        }

        playerGold -= amount;
        OnGoldChanged?.Invoke(playerGold);
        Debug.Log($"玩家消费 {amount} 金币，剩余金币: {playerGold}");
        return true;
    }

    public void SetPlayerGold(int amount)
    {
        playerGold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(playerGold);
    }

    public void ResetPlayerData()
    {
        playerGold = 0;
        OnGoldChanged?.Invoke(playerGold);
        Debug.Log("玩家数据已重置");
    }

    [ContextMenu("Test Player Gold")]
    public void test()
    {
        playerGold = 100;
    }
    #endregion

    #region 道具管理

    // 道具效果管理方法
    public void SetCheerBoostActive(bool active)
    {
        cheerBoostActive = active;
        Debug.Log($"欢呼加速效果设置为: {active}");
    }

    public void SetExtraRewardActive(bool active)
    {
        extraRewardActive = active;
        Debug.Log($"额外奖励效果设置为: {active}");
    }

   
    public void SetExtraLifeActive(bool active)
    {
        extraLifeActive = active;
        Debug.Log($"额外生命效果设置为: {active}");

        if (active)
        {
            // 查找场景中所有的 Controller
            RhythmKeyControllerBase[] controllers = FindObjectsOfType<RhythmKeyControllerBase>();
            foreach (RhythmKeyControllerBase controller in controllers)
            {
                controller.failToLose += 1;
                Debug.Log($"为 {controller.gameObject.name} 增加一次失败机会，当前失败次数限制: {controller.failToLose}");
            }
            LongPressController longPressController = FindObjectOfType<LongPressController>();
            longPressController.failToLose += 1;
        }
    }

    public bool IsExtraLifeActive() => extraLifeActive;
    // 添加长按模式管理方法
    public void SetHoldModeActive(bool active)
    {
        holdModeActive = active;
        Debug.Log($"长按模式效果设置为: {active}");
    }

    public bool IsHoldModeActive() => holdModeActive;

// 自动游戏效果
    public void SetAutoPlayActive(bool active)
    {
        autoPlayActive = active;
        Debug.Log($"自动游戏效果设置为: {active}");
    }

    public bool IsAutoPlayActive() => autoPlayActive;

// 获取道具效果状态
    public bool IsCheerBoostActive() => cheerBoostActive;
    public bool IsExtraRewardActive() => extraRewardActive;
 
// 重置所有道具效果
    public void ResetAllItemEffects()
    {
        cheerBoostActive = false;
        extraRewardActive = false;
        autoPlayActive = false;
        extraLifeActive = false;
        Debug.Log("所有道具效果已重置");
    }
    public void ReapplyAllActiveItems()
    {
        // 重新应用额外生命效果
        if (extraLifeActive)
        {
            RhythmKeyControllerBase[] controllers = FindObjectsOfType<RhythmKeyControllerBase>();
            foreach (RhythmKeyControllerBase controller in controllers)
            {
                controller.failToLose *= 2;
                Debug.Log($"重新应用额外生命效果：为 {controller.gameObject.name} 增加失败次数，当前限制: {controller.failToLose}");
            }
    
            var longPressController = FindObjectOfType<LongPressController>();
            if (longPressController != null)
            {
                longPressController.failToLose += 1;
            }
        }

        // 重新应用欢呼加速效果
        if (cheerBoostActive)
        {
            var progressBar = FindObjectOfType<ProgressBarController>();
            if (progressBar != null && progressBar.isFilling)
            {
                progressBar.StopFilling();
                progressBar.StartFilling();
                Debug.Log("重新应用欢呼加速效果");
            }
        }

        // 重新应用额外奖励效果
        if (extraRewardActive)
        {
            var progressBar = FindObjectOfType<ProgressBarController>();
            if (progressBar != null)
            {
                progressBar.numberOfCoins += 5;
                Debug.Log($"重新应用额外奖励效果：完成后可获得金币数增加到 {progressBar.numberOfCoins}");
            }
        }
        if (holdModeActive)
        {
            var controllers = FindObjectsOfType<RhythmKeyControllerBase>();
            foreach (var controller in controllers)
            {
                if (controller.gameObject.name == "RightClick")
                {
                    controller.EnableHoldMode();
                    Debug.Log("重新应用长按模式效果");
                    break;
                }
            }
        }
        Debug.Log("已重新应用所有激活的道具效果");
    }
    #endregion
}