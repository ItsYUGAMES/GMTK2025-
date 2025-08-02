using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public int itemPrice;
    public Sprite itemIcon;
    public string itemDescription;
    public ItemEffect itemEffect;
    public ItemType itemType;

    public void InitializeFromItemEffect(ItemEffect effect)
    {
        if (effect != null)
        {
            itemEffect = effect;
            itemName = effect.itemName;
            itemPrice = effect.itemPrice;
            itemDescription = effect.itemDescription;
            itemIcon = effect.itemIcon;
            itemType = effect.itemType;
        }
    }
}