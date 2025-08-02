
    using UnityEngine;
    

    /// <summary>
    /// 自动游戏道具
    /// </summary>
    [CreateAssetMenu(fileName = "Auto Play Item", menuName = "Shop/Items/Auto Play")]
    public class AutoPlayItem : ItemEffect
    {
        [UnityEngine.Header("道具效果")]
        public float autoClickInterval = 0.5f;  // 自动点击间隔（秒）

        private void OnEnable()
        {
            itemName = "自动游戏";
            itemDescription = "自动进行游戏";
            itemPrice = 15;

            isPermanent = true;
        }

        public override void OnPurchase()
        {
            Debug.Log($"购买了 {itemName}，启用自动游戏模式");

            // 通过PlayerDataManager设置效果状态
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.SetAutoPlayActive(true);
            }
            base.OnPurchase();

        }

        public override string GetDetailedDescription()
        {
            return $"每 {autoClickInterval} 秒自动点击一次";
        }
    }
    