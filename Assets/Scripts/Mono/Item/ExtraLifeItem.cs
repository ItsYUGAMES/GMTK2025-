using UnityEngine;

/// <summary>
/// ����һ��ʧ�������� (5���)
/// </summary>
[CreateAssetMenu(fileName = "Extra Life Item", menuName = "Shop/Items/Extra Life")]
public class ExtraLifeItem : ItemEffect
{
    [Header("����Ч��")]
    public int extraLives = 1;

    private void OnEnable()
    {
        itemName = "����һ��ʧ�����";
        itemDescription = "����1��ʧ�����";
        itemPrice = 5;
        isConsumable = true;  // ���Զ�ι���
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"������ {itemName}�������� {extraLives} ��ʧ�����");

        RhythmKeyControllerBase[] controllers = FindObjectsOfType<RhythmKeyControllerBase>();

        if (controllers.Length == 0)
        {
            SaveEffectForLater();
        }
        else
        {
            foreach (var controller in controllers)
            {
                controller.AddExtraLife(extraLives);
            }
        }
    }

    private void SaveEffectForLater()
    {
        int currentPending = PlayerPrefs.GetInt("PendingExtraLives", 0);
        PlayerPrefs.SetInt("PendingExtraLives", currentPending + extraLives);
        PlayerPrefs.Save();
        Debug.Log($"������ {extraLives} ����Ӧ�õĶ�������");
    }
}