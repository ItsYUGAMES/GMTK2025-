using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgressBarController : MonoBehaviour
{
    public Image progressBarImage;
    public float fillDuration = 10f;

    [Header("金币掉落设置")]
    public GameObject coinPrefab;
    public int numberOfCoins = 10;
    public float spawnRadius = 2f; // 金币生成的水平散布范围
    // public float spawnHeight = 5f; // 不再需要这个参数，因为我们将基于屏幕顶部计算位置

    public float spawnForceMin = 3f;
    public float spawnForceMax = 6f;

    // 新增：金币在屏幕顶部的 Y 轴偏移量 (负值表示在顶部以下一点，正值表示在顶部以上一点)
    public float screenTopOffset = 1f;

    [Header("商店设置")]
    public ShopManager shopManager; // 引用 ShopManager

    private float timer = 0f;
    private bool isFilling = false;
    private Camera mainCamera; // 引用主摄像机
    private int activeCoinsCount = 0; // 追踪活跃的硬币数量

    void Awake() // 使用 Awake 来获取摄像机引用，确保在 Start 之前可用
    {
        mainCamera = Camera.main; // 获取主摄像机引用
        if (mainCamera == null)
        {
            Debug.LogError("场景中没有找到带有 'MainCamera' 标签的摄像机！请确保主摄像机设置了此标签。", this);
        }
    }

    void Start()
    {
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("ProgressBar Image 未设置！请在 Inspector 中拖拽 FanProgressBar Image 组件。", this);
        }

        if (coinPrefab == null)
        {
            Debug.LogError("Coin Prefab 未设置！金币将无法掉落。", this);
        }

        // StartFilling(); // 可以在游戏开始时立即启动填充
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isFilling)
        {
            StartFilling();
        }
    }

    public void StartFilling()
    {
        if (progressBarImage == null) return;
        if (isFilling) return;

        timer = 0f;
        progressBarImage.fillAmount = 0f;
        isFilling = true;
        StartCoroutine(FillProgressBarCoroutine());
    }

    public void StopFilling()
    {
        if (isFilling)
        {
            StopAllCoroutines();
            isFilling = false;
            Debug.Log("进度条填充已停止。");
        }
    }

    IEnumerator FillProgressBarCoroutine()
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < fillDuration)
        {
            elapsedTime = Time.time - startTime;
            progressBarImage.fillAmount = Mathf.Clamp01(elapsedTime / fillDuration);
            yield return null;
        }

        progressBarImage.fillAmount = 1f;
        isFilling = false;
        Debug.Log("进度条填充完成！");

        StartCoroutine(SpawnCoinsRoutine());
    }

    IEnumerator SpawnCoinsRoutine()
    {
        Debug.Log($"开始掉落 {numberOfCoins} 个金币...");
        activeCoinsCount = numberOfCoins; // 设置活跃硬币数量

        for (int i = 0; i < numberOfCoins; i++)
        {
            SpawnSingleCoin();
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// 生成单个金币并赋予初始推力。
    /// 金币现在将从屏幕上方掉落。
    /// </summary>
    void SpawnSingleCoin()
    {
        if (coinPrefab == null || mainCamera == null) return;

        // 计算屏幕右上角的世界坐标
        // ViewportToWorldPoint(Vector3(0, 1, distance)) 表示屏幕左上角的世界坐标
        // ViewportToWorldPoint(Vector3(1, 1, distance)) 表示屏幕右上角的世界坐标
        // 我们需要一个 Z 深度值，对于 2D 游戏，通常是 0 或者与摄像机 Z 距离相关的某个值
        // 这里假设 Z 轴为 0 或者使用金币的默认 Z 轴
        float cameraZDistance = -mainCamera.transform.position.z; // 如果摄像机是 -10，距离就是 10

        // 获取屏幕顶部边缘的 Y 坐标（世界坐标）
        Vector3 screenTopLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, cameraZDistance));
        Vector3 screenTopRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraZDistance));

        // 确定金币的生成 Y 轴位置 (在屏幕顶部之上或之内)
        float spawnY = screenTopLeft.y + screenTopOffset; // 加上一个偏移量，让金币从屏幕上方一点出现

        // 确定金币的生成 X 轴范围 (屏幕宽度)
        float minX = screenTopLeft.x;
        float maxX = screenTopRight.x;

        // 在 X 轴上随机选择一个位置，并在 Y 轴上固定为屏幕顶部附近
        float randomX = Random.Range(minX + spawnRadius, maxX - spawnRadius); // 在屏幕宽度内，留出边缘的散布空间

        Vector3 spawnPos = new Vector3(randomX, spawnY, 0); // Z 轴设为 0 (或其他适合 2D 的值)

        // 实例化金币
        GameObject newCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

        // 为金币添加销毁监听
        Coin coinScript = newCoin.GetComponent<Coin>();
        if (coinScript == null)
        {
            coinScript = newCoin.AddComponent<Coin>();
        }

        // 设置硬币销毁时的回调
        coinScript.SetDestroyCallback(OnCoinDestroyed);

        // 获取金币的 Rigidbody2D 组件并施加一个向下的初始推力
        Rigidbody2D rb = newCoin.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float forceX = Random.Range(-1f, 1f); // 随机水平推力
            float forceY = Random.Range(spawnForceMin, spawnForceMax); // 垂直推力
            rb.AddForce(new Vector2(forceX, -forceY), ForceMode2D.Impulse); // Impulse 模式施加瞬间力
        }
    }

    // 硬币销毁时的回调
    public void OnCoinDestroyed()
    {
        activeCoinsCount--;
        Debug.Log($"硬币被销毁，剩余硬币数量: {activeCoinsCount}");

        // 如果所有硬币都被销毁，打开商店
        if (activeCoinsCount <= 0)
        {
            Debug.Log("所有硬币已收集完毕，打开商店！");
            if (shopManager != null)
            {
                shopManager.OpenShop();
            }
            else
            {
                Debug.LogError("ShopManager 未设置！请在 Inspector 中拖拽 ShopManager。");
            }
        }
    }
}