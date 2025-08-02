using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 尽管不再使用Image，但为了兼容性保留

public class Win : MonoBehaviour
{
    // 移除与进度条相关的公共变量
    
    [Header("金币掉落设置")]
    public GameObject coinPrefab;
    public int numberOfCoins = 10;
    private int baseNumberOfCoins; // 保存原始金币数量
    public float spawnRadius = 2f;
    public float spawnForceMin = 3f;
    public float spawnForceMax = 6f;
    public float screenTopOffset = 1f;

    [Header("商店设置")]
    public ShopManager shopManager;

    [Header("Canvas移动设置")]
    public Transform upObject;
    public Transform downObject;
    public float targetY = 0f; // 该变量目前未被使用，但保留
    public float moveSpeed = 2f;

    // 移除脚本禁用设置，这部分逻辑可以在 GameManager 中更集中地处理
    
    private Camera mainCamera;
    private int activeCoinsCount = 0;
    private bool isMoving = false;
    private bool isPaused = false; // 保持暂停状态，以便在协程中检查

    public GameManager gameManager;

    [Header("音效设置")]
    public AudioClip levelCompletedSFX;

    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("场景中没有找到带有 'MainCamera' 标签的摄像机！", this);
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }

        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                Debug.Log($"自动找到ShopManager: {shopManager.name}");
            }
            else
            {
                Debug.LogWarning("场景中没有找到ShopManager组件");
            }
        }

        // 保存原始金币数量
        baseNumberOfCoins = numberOfCoins;

        FindUpAndDownObjects();
    }

    void Start()
    {
        // 检查是否有额外奖励
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsExtraRewardActive())
        {
            numberOfCoins = baseNumberOfCoins + 5;
            Debug.Log($"应用额外奖励，金币数量: {numberOfCoins}");
        }
        else
        {
            numberOfCoins = baseNumberOfCoins;
        }

        if (coinPrefab == null)
        {
            Debug.LogError("Coin Prefab 未设置！金币将无法掉落。", this);
            return;
        }
        
        // **核心改动：在 Start() 中直接开始金币掉落和对象移动**
        StartCoroutine(SpawnCoinsAndMoveObjects());
    }

    // 组合金币掉落和对象移动的协程
    IEnumerator SpawnCoinsAndMoveObjects()
    {
        // 同时启动金币掉落和移动Up/Down对象的协程
        StartCoroutine(SpawnCoinsRoutine());
        StartCoroutine(MoveCanvasAndOpenShop());
        
        // 这个协程可以结束了，因为它已经启动了其他两个协程
        yield break;
    }
    
    // 保留暂停和恢复方法
    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;

        if (isPaused)
        {
            Debug.Log("ProgressBarController 已暂停");
        }
        else
        {
            Debug.Log("ProgressBarController 已恢复");
        }
    }

    // ... (FindUpAndDownObjects, SpawnCoinsRoutine, SpawnSingleCoin, OnCoinDestroyed, MoveCanvasAndOpenShop 方法保持不变) ...

    private void FindUpAndDownObjects()
    {
        if (upObject == null)
        {
            GameObject upObj = GameObject.Find("Up");
            if (upObj != null)
            {
                upObject = upObj.transform;
                Debug.Log("找到Up对象: " + upObj.name);
            }
        }

        if (downObject == null)
        {
            GameObject downObj = GameObject.Find("Down");
            if (downObj != null)
            {
                downObject = downObj.transform;
                Debug.Log("找到Down对象: " + downObj.name);
            }
        }

        if (upObject == null || downObject == null)
        {
            Debug.LogWarning("未找到Up或Down对象");
        }
    }
    
    IEnumerator SpawnCoinsRoutine()
    {
        Debug.Log($"开始掉落 {numberOfCoins} 个金币...");
        activeCoinsCount = numberOfCoins;
    
        for (int i = 0; i < numberOfCoins; i++)
        {
            yield return new WaitUntil(() => !isPaused);
            SpawnSingleCoin();
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    void SpawnSingleCoin()
    {
        if (coinPrefab == null || mainCamera == null) return;
    
        float cameraZDistance = -mainCamera.transform.position.z;
        Vector3 screenTopLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, cameraZDistance));
        Vector3 screenTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraZDistance));
    
        float spawnY = screenTopLeft.y + screenTopOffset;
        float minX = screenTopLeft.x;
        float maxX = screenTopRight.x;
    
        float randomX = Random.Range(minX + spawnRadius, maxX - spawnRadius);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);
    
        GameObject newCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
    
        Coin coinScript = newCoin.GetComponent<Coin>();
        if (coinScript == null)
        {
            coinScript = newCoin.AddComponent<Coin>();
        }
    
        coinScript.SetDestroyCallback(OnCoinDestroyed);
    
        Rigidbody2D rb = newCoin.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float forceX = Random.Range(-1f, 1f);
            float forceY = Random.Range(spawnForceMin, spawnForceMax);
            rb.AddForce(new Vector2(forceX, -forceY), ForceMode2D.Impulse);
        }
    }
    
    public void OnCoinDestroyed()
    {
        activeCoinsCount--;
        Debug.Log($"硬币被销毁，剩余硬币数量: {activeCoinsCount}");
    
        if (activeCoinsCount <= 0 && !isMoving)
        {
            Debug.Log("所有硬币已收集完毕，准备打开商店！");
            // 这里不再需要调用 MoveCanvasAndOpenShop，因为它已经由 StartCoroutine 启动了
            // 现在只需要确保 isMoving 标志正确处理，以在协程中等待
        }
    }
    
    private IEnumerator MoveCanvasAndOpenShop()
    {
        if (upObject != null && downObject != null)
        {
            isMoving = true;
            Debug.Log("开始移动Sprite对象到世界坐标(0,0,0)");
    
            Vector3 upStartPos = upObject.position;
            Vector3 downStartPos = downObject.position;
            Vector3 targetPos = Vector3.zero;
    
            float journey = 0f;
    
            while (journey <= 1f)
            {
                yield return new WaitUntil(() => !isPaused);
                journey += Time.deltaTime * moveSpeed;
                upObject.position = Vector3.Lerp(upStartPos, targetPos, journey);
                downObject.position = Vector3.Lerp(downStartPos, targetPos, journey); // 修正：downObject.position也应该向targetPos移动
                yield return null;
            }
    
            upObject.position = targetPos;
            downObject.position = targetPos;
            Debug.Log("Sprite对象已移动到世界坐标(0,0,0)");
            isMoving = false;
        }
    
        SFXManager.Instance.PlaySFX(levelCompletedSFX);
    }
    public void RestartGame()
    {
        Debug.Log("按钮被点击了！");
        Debug.Log("重启游戏，返回GameStart场景");
        SceneManager.LoadScene("GameStart");
    }
    // ... (精简了公共方法，只保留了ResetProgress) ...

    public void ResetProgress()
    {
        // 移除与进度条相关的重置
        activeCoinsCount = 0;
        isMoving = false;
        isPaused = false;
    }

    // 移除所有与进度条填充相关的私有方法和协程
}