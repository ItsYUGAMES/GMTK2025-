using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ShopItemUI shopItemUI;
    private float hoverDelay;
    private Coroutine hoverCoroutine;
    private bool isHovering = false;
    private bool isTooltipShowing = false;
    private bool isStable = false; // 添加稳定状态标记

    public void Initialize(ShopItemUI shopUI, float delay)
    {
        shopItemUI = shopUI;
        hoverDelay = delay;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"鼠标进入 HoverPanel - GameObject: {gameObject.name}");
        isHovering = true;

        // 如果已经在显示且稳定，直接返回
        if (isTooltipShowing && isStable)
        {
            Debug.Log("提示已在稳定显示，直接返回");
            return;
        }

        // 停止之前的显示协程
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"鼠标离开 HoverPanel - GameObject: {gameObject.name}");
        
        // 添加小延迟，防止因为提示框遮挡导致的误触发
        StartCoroutine(DelayedExit());
    }

    private IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(0.1f); // 短暂延迟
        
        // 重新检查鼠标位置
        Vector2 mousePosition = Input.mousePosition;
        RectTransform rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null && !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition))
        {
            isHovering = false;
            isStable = false;

            // 停止显示协程
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
                hoverCoroutine = null;
                Debug.Log("停止显示协程");
            }

            // 隐藏提示框
            if (isTooltipShowing)
            {
                Debug.Log("执行隐藏提示框");
                isTooltipShowing = false;
                if (shopItemUI != null)
                {
                    shopItemUI.HideTooltip();
                }
            }
        }
        else
        {
            Debug.Log("鼠标仍在范围内，取消退出");
        }
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        Debug.Log($"开始等待 {hoverDelay} 秒显示提示");

        float elapsed = 0f;
        while (elapsed < hoverDelay)
        {
            if (!isHovering)
            {
                Debug.Log($"等待期间鼠标离开，取消显示 - elapsed: {elapsed}");
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 最终检查
        if (isHovering && shopItemUI != null)
        {
            Debug.Log("执行 ShowTooltip");
            isTooltipShowing = true;
            shopItemUI.ShowTooltip();
            
            // 等待一帧后设置为稳定状态
            yield return new WaitForEndOfFrame();
            isStable = true;
        }
        else
        {
            Debug.Log($"最终检查失败 - isHovering: {isHovering}, shopItemUI: {shopItemUI != null}");
        }
        hoverCoroutine = null;
    }

    public void Cleanup()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        isTooltipShowing = false;
        isHovering = false;
        isStable = false;
    }
}