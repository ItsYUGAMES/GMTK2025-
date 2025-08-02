using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ProgressBarController : MonoBehaviour
{
    public Image progressBarImage;
    public float fillDuration = 10f;
    private float originalFillDuration; // 保存原始填充时间

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
    public float targetY = 0f;
    public float moveSpeed = 2f;

    [Header("脚本禁用设置")]
    [SerializeField] private List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();
    
    public string[] scriptsToKeep = {
        "GameManager",
        "ShopManager",
        "SFXManager",
        "PlayerDataManager",
        "ProgressBarController"
    };

    private float timer = 0f;
    public bool isFilling = false;
    private bool isPaused = false; // 暂停状态
    private Camera mainCamera;
    private int activeCoinsCount = 0;
    private bool isMoving = false;

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

        // 自动获取 GameManager
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }

        // 自动获取 ShopManager
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

        // 保存原始值
        originalFillDuration = fillDuration;
        baseNumberOfCoins = numberOfCoins;

        // 自动查找Up和Down对象
        FindUpAndDownObjects();
    }

    void Start()
    {
        // 检查道具效果并应用
        if (PlayerDataManager.Instance != null)
        {
            // 检查是否有欢呼加速效果
            if (PlayerDataManager.Instance.IsCheerBoostActive())
            {
                fillDuration = originalFillDuration * 0.7f; // 加速30%
                Debug.Log($"应用欢呼加速效果，填充时间: {fillDuration} 秒");
            }
            else
            {
                fillDuration = originalFillDuration;
            }

            // 检查是否有额外奖励
            if (PlayerDataManager.Instance.IsExtraRewardActive())
            {
                numberOfCoins = baseNumberOfCoins + 5; // 额外5个金币
                Debug.Log($"应用额外奖励，金币数量: {numberOfCoins}");
            }
            else
            {
                numberOfCoins = baseNumberOfCoins;
            }
        }

        // 其余初始化代码...
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("ProgressBar Image 未设置！", this);
        }

        if (coinPrefab == null)
        {
            Debug.LogError("Coin Prefab 未设置！金币将无法掉落。", this);
        }

        StartFilling();
    }

    void OnEnable()
    {
        // 组件被启用时，如果之前被暂停，则恢复
        if (isPaused)
        {
            SetPaused(false);
            Debug.Log("ProgressBarController 已重新启用并恢复");
        }
    }

    void OnDisable()
    {
        // 组件被禁用时，自动暂停
        if (isFilling)
        {
            SetPaused(true);
            Debug.Log("ProgressBarController 已被禁用并暂停");
        }
    }

    // 暂停和恢复方法
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

    private void FindUpAndDownObjects()
    {
        if (upObject == null)
        {
            GameObject upObj = GameObject.Find("Up");
            if (upObj != null)
            {
                upObject = upObj.transform;
                Debug.Log("ProgressBar找到Up对象: " + upObj.name);
            }
        }

        if (downObject == null)
        {
            GameObject downObj = GameObject.Find("Down");
            if (downObj != null)
            {
                downObject = downObj.transform;
                Debug.Log("ProgressBar找到Down对象: " + downObj.name);
            }
        }

        if (upObject == null)
        {
            Debug.LogWarning("未找到Up对象，请确保场景中存在名为'Up'的GameObject");
        }

        if (downObject == null)
        {
            Debug.LogWarning("未找到Down对象，请确保场景中存在名为'Down'的GameObject");
        }
    }

    public void StartFilling()
    {
        if (progressBarImage == null) return;
        if (isFilling) return;

        timer = 0f;
        progressBarImage.fillAmount = 0f;
        isFilling = true;
        isPaused = false;
        StartCoroutine(FillProgressBarCoroutine());
    }

    public void StopFilling()
    {
        if (isFilling)
        {
            StopAllCoroutines();
            isFilling = false;
            isPaused = false;
            Debug.Log("进度条填充已停止。");
        }
    }

    public void SetFillSpeed(float speedMultiplier)
    {
        fillDuration = originalFillDuration * speedMultiplier;
        Debug.Log($"进度条填充速度已调整，新的填充时间: {fillDuration} 秒");

        // 如果正在填充，重新开始以应用新速度
        if (isFilling)
        {
            StopFilling();
            StartFilling();
        }
    }

    public void AddExtraCoins(int extraCoins)
    {
        numberOfCoins += extraCoins;
        Debug.Log($"增加了 {extraCoins} 个额外金币，总金币数: {numberOfCoins}");
    }

    public void ResetToDefaults()
    {
        fillDuration = originalFillDuration;
        numberOfCoins = baseNumberOfCoins;
        Debug.Log("进度条设置已重置为默认值");
    }

    IEnumerator FillProgressBarCoroutine()
    {
        float startTime = Time.time;
        float elapsedTime = 0f;
        float pausedDuration = 0f;

        while (elapsedTime < fillDuration)
        {
            // 检查组件是否被禁用或暂停
            if (!enabled || isPaused)
            {
                float pauseStartTime = Time.time;
                // 等待组件重新启用且不暂停
                yield return new WaitUntil(() => enabled && !isPaused);
                pausedDuration += Time.time - pauseStartTime;
            }

            elapsedTime = Time.time - startTime - pausedDuration;
            progressBarImage.fillAmount = Mathf.Clamp01(elapsedTime / fillDuration);
            yield return null;
        }

        progressBarImage.fillAmount = 1f;
        isFilling = false;
        Debug.Log("进度条填充完成！");

        // 播放完成音效
        if (SFXManager.Instance != null && levelCompletedSFX != null)
        {
            SFXManager.Instance.PlaySFX(levelCompletedSFX);
        }

        // 进度条填满后禁用自定义脚本
        DisableCustomScripts();

        StartCoroutine(SpawnCoinsRoutine());
    }

    // 禁用自定义脚本的方法
    private void DisableCustomScripts()
    {
        // 禁用直接引用的脚本
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
                Debug.Log($"已禁用指定脚本: {script.GetType().Name} (GameObject: {script.gameObject.name})");
            }
        }

        // 如果没有指定脚本，则使用原来的通用方法
        if (scriptsToDisable.Count == 0)
        {
            MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();

            foreach (MonoBehaviour script in allScripts)
            {
                if (script == null || script == this) continue;

                string scriptType = script.GetType().Name;

                // 跳过Unity内置组件
                if (IsUnityBuiltInComponent(scriptType))
                    continue;

                // 检查是否在保护列表中
                if (System.Array.Exists(scriptsToKeep, keepScript => keepScript == scriptType))
                {
                    Debug.Log($"保护脚本: {scriptType} (GameObject: {script.gameObject.name})");
                    continue;
                }

                script.enabled = false;
                Debug.Log($"已禁用自定义脚本: {scriptType} (GameObject: {script.gameObject.name})");
            }
        }
    }

    // 检查是否是Unity内置组件
    private bool IsUnityBuiltInComponent(string componentName)
    {
        string[] unityComponents = {
            "Canvas", "CanvasScaler", "GraphicRaycaster", "CanvasRenderer",
            "Image", "Text", "Button", "ScrollRect", "Slider", "Toggle",
            "InputField", "Dropdown", "RawImage", "Mask", "RectMask2D",
            "ContentSizeFitter", "LayoutElement", "HorizontalLayoutGroup",
            "VerticalLayoutGroup", "GridLayoutGroup", "AspectRatioFitter",
            "Camera", "Light", "MeshRenderer", "MeshFilter", "Collider",
            "Collider2D", "Rigidbody", "Rigidbody2D", "Transform", "RectTransform",
            "AudioSource", "AudioListener", "ParticleSystem", "Animation",
            "Animator", "SpriteRenderer", "LineRenderer", "TrailRenderer"
        };

        return System.Array.Exists(unityComponents, component => component == componentName);
    }

    IEnumerator SpawnCoinsRoutine()
    {
        Debug.Log($"开始掉落 {numberOfCoins} 个金币...");
        activeCoinsCount = numberOfCoins;

        for (int i = 0; i < numberOfCoins; i++)
        {
            // 检查组件是否被禁用或暂停
            yield return new WaitUntil(() => enabled && !isPaused);

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
            Debug.Log("所有硬币已收集完毕，开始移动Canvas对象并打开商店！");
            StartCoroutine(MoveCanvasAndOpenShop());
        }
    }

    private System.Collections.IEnumerator MoveCanvasAndOpenShop()
    {
        if (upObject != null && downObject != null)
        {
            isMoving = true;
            Debug.Log("所有硬币已被销毁，开始移动Sprite对象到世界坐标(0,0,0)");

            Vector3 upStartPos = upObject.position;
            Vector3 downStartPos = downObject.position;

            Vector3 targetPos = Vector3.zero; // 世界坐标(0,0,0)

            float journey = 0f;

            while (journey <= 1f)
            {
                // 检查组件是否被禁用或暂停
                if (!enabled || isPaused)
                {
                    yield return new WaitUntil(() => enabled && !isPaused);
                }

                journey += Time.deltaTime * moveSpeed;

                upObject.position = Vector3.Lerp(upStartPos, targetPos, journey);
                downObject.position = Vector3.Lerp(downStartPos, targetPos, journey);

                yield return null;
            }

            upObject.position = targetPos;
            downObject.position = targetPos;

            Debug.Log("Sprite对象已移动到世界坐标(0,0,0)");
        }

        // 移动完成后打开商店
        if (shopManager != null && gameManager.CheckGameplayScene())
        {
            if (GameManager.Instance.isSingleMode == true)
            {
                shopManager.OpenShop();
            }
            else
            {
                gameManager.LoadNextLevel();
            }
            
        }
        else
        {
            Debug.LogError("ShopManager 未设置！");
        }
    }

    // 公共方法供外部调用
    public float GetProgress()
    {
        return progressBarImage != null ? progressBarImage.fillAmount : 0f;
    }

    public bool IsFillingComplete()
    {
        return !isFilling && progressBarImage != null && progressBarImage.fillAmount >= 1f;
    }

    public void ResetProgress()
    {
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        timer = 0f;
        isFilling = false;
        activeCoinsCount = 0;
        isMoving = false;
        isPaused = false;
    }
}