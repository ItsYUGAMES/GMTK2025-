using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI 组件")]
    public Image itemIcon;
    public Button buyButton;

    [Header("悬浮提示设置")]
    public GameObject hoverPanel;          // 用于检测悬浮的面板
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public float hoverDelay = 1f;

    private ShopItem currentItem;
    private HoverHandler hoverHandler;

    void Start()
    {
        SetupHoverPanel();
    }

    private void SetupHoverPanel()
    {
        if (hoverPanel != null)
        {
            // 确保 hoverPanel 有 Image 组件且 Raycast Target 为 true
            Image hoverImage = hoverPanel.GetComponent<Image>();
            if (hoverImage == null)
            {
                hoverImage = hoverPanel.AddComponent<Image>();
                hoverImage.color = Color.clear; // 设置为透明
            }
            hoverImage.raycastTarget = true;

            // 添加悬浮处理器
            hoverHandler = hoverPanel.GetComponent<HoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = hoverPanel.AddComponent<HoverHandler>();
            }
            hoverHandler.Initialize(this, hoverDelay);
        }
        else
        {
            Debug.LogError("HoverPanel 未设置！");
        }
    }

    public void SetItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogError("ShopItem 为 null！");
            return;
        }

        currentItem = item;

        // 更新图标
        if (itemIcon != null && item.itemIcon != null)
            itemIcon.sprite = item.itemIcon;
        else if (itemIcon != null)
            Debug.LogWarning("item.itemIcon 未设置！");

        // 确保提示面板初始状态为隐藏
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void ShowTooltip()
    {
        Debug.Log("ShowTooltip 被调用");
    
        if (tooltipPanel == null)
        {
            Debug.LogError("tooltipPanel 为 null！");
            return;
        }
    
        if (tooltipText == null)
        {
            Debug.LogError("tooltipText 为 null！");
            return;
        }
    
        if (currentItem == null)
        {
            Debug.LogError("currentItem 为 null！");
            return;
        }

        // 设置提示文本内容
        string description = "";
        if (currentItem.itemEffect != null)
        {
            description = currentItem.itemEffect.GetDetailedDescription();
        }
        else
        {
            description = currentItem.itemDescription;
        }

        Debug.Log($"设置描述文本: {description}");
        tooltipText.text = description;
    
        // 确保在最顶层显示
        tooltipPanel.transform.SetAsLastSibling();
        tooltipPanel.SetActive(true);
    
        Debug.Log($"tooltipPanel 激活状态: {tooltipPanel.activeInHierarchy}");

        // 使用 Canvas 坐标而不是屏幕坐标
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Vector2 screenPosition = Input.mousePosition;
            Vector2 canvasPosition;
        
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.worldCamera,
                out canvasPosition
            );
        
            tooltipPanel.transform.localPosition = canvasPosition + new Vector2(10, 10);
            Debug.Log($"设置提示框位置: {canvasPosition}");
        }
        else
        {
            Debug.LogError("找不到 Canvas！");
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (hoverHandler != null)
        {
            hoverHandler.Cleanup();
        }
    }
}