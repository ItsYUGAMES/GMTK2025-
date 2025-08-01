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
    public float spawnRadius = 2f;
    public float spawnForceMin = 3f;
    public float spawnForceMax = 6f;
    public float screenTopOffset = 1f;

    [Header("商店设置")]
    public ShopManager shopManager;

    [Header("Canvas移动设置")]
    public Transform upObject;
    public Transform downObject;
    public float targetY = -490f;
    public float moveSpeed = 2f;

    private float timer = 0f;
    private bool isFilling = false;
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

        // 自动查找Up和Down对象
        FindUpAndDownObjects();
    }

    void Start()
    {
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

        if (SFXManager.Instance != null && levelCompletedSFX != null)
        {
            SFXManager.Instance.PlaySFX(levelCompletedSFX);
        }

        StartCoroutine(SpawnCoinsRoutine());
    }

    IEnumerator SpawnCoinsRoutine()
    {
        Debug.Log($"开始掉落 {numberOfCoins} 个金币...");
        activeCoinsCount = numberOfCoins;

        for (int i = 0; i < numberOfCoins; i++)
        {
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
            shopManager.OpenShop();
        }
        else
        {
            Debug.LogError("ShopManager 未设置！");
        }
    }
}