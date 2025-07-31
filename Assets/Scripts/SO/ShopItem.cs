using UnityEngine;

// System.Serializable 允许这个类在 Inspector 中显示和编辑
[System.Serializable]
public class ShopItem
{
    public string itemName;      // 商品名称
    public int itemPrice;        // 商品价格
    public Sprite itemIcon;      // 商品图标
    // 你可以根据需要添加更多属性，例如：
    // public string itemDescription;
    // public int itemID;
}