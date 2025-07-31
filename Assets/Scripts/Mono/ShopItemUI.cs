using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果你使用 TextMeshPro

public class ShopItemUI : MonoBehaviour
{
    [Header("UI 组件")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIcon;
    public Button buyButton;

    public void SetItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogError("ShopItem 为 null！");
            return;
        }

        // 更新文本
        if (itemNameText != null)
            itemNameText.text = item.itemName;
        else
            Debug.LogError("itemNameText 未设置！");

        if (itemPriceText != null)
            itemPriceText.text = $"价格: {item.itemPrice}";
        else
            Debug.LogError("itemPriceText 未设置！");

        // 更新图片
        if (itemIcon != null && item.itemIcon != null)
            itemIcon.sprite = item.itemIcon;
        else
            Debug.LogError("itemIcon 或 item.itemIcon 未设置！");
    }
}