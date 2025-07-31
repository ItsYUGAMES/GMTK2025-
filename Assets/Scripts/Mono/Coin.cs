using UnityEngine;
using System;

public class Coin : MonoBehaviour
{
    [Header("硬币设置")]
    public int coinValue = 10;
    public float destroyBelowY = -10f; // 当金币Y坐标低于此值时销毁

    private Action onDestroyCallback; // 销毁时的回调
    private Camera mainCamera;
    public bool isDestroyed = false; // 防止重复销毁
    private bool isCollected = false; // 防止重复收集

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    private void Start()
    {
        // 确保有 Rigidbody2D 用于物理掉落
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
        }
    }

    public void SetDestroyCallback(Action callback)
    {
        onDestroyCallback = callback;
    }

    private void Update()
    {
        // 检查金币是否掉出屏幕
        if (!isDestroyed && ShouldDestroy())
        {
            DestroyCoin();
        }
    }

    public void CollectCoin()
    {
        if (isCollected || isDestroyed) return;

        isCollected = true;
        Debug.Log($"玩家收集硬币，获得 {coinValue} 金币");

        // 增加玩家金币
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.AddPlayerGold(coinValue);
        }

        DestroyCoin();
    }

    private bool ShouldDestroy()
    {
        if (mainCamera != null)
        {
            // 获取屏幕底部的世界坐标
            float cameraZDistance = -mainCamera.transform.position.z;
            Vector3 screenBottom = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0, cameraZDistance));

            // 如果金币掉到屏幕底部以下一定距离就销毁
            return transform.position.y < screenBottom.y - 2f;
        }
        else
        {
            // 如果没有摄像机引用，使用固定的Y坐标
            return transform.position.y < destroyBelowY;
        }
    }

    private void DestroyCoin()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log($"硬币 {gameObject.name} 被销毁");

        // 调用销毁回调
        onDestroyCallback?.Invoke();

        // 销毁硬币
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 确保回调被调用（防止其他方式销毁时遗漏）
        if (!isDestroyed && onDestroyCallback != null)
        {
            onDestroyCallback.Invoke();
        }
    }
}   