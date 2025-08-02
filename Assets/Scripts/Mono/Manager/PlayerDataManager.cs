using System;
using UnityEngine;

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
    }

    public bool IsExtraLifeActive() => extraLifeActive;

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

    #endregion
}