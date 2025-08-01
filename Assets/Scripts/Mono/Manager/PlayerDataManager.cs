using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("玩家数据")]
    [SerializeField] private int playerGold = 0;

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
}