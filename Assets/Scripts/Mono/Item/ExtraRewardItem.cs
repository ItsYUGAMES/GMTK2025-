using UnityEngine;

/// <summary>
/// ��ɻ�ö�����͵��� (5���)
/// </summary>
[CreateAssetMenu(fileName = "Extra Reward Item", menuName = "Shop/Items/Extra Reward")]
public class ExtraRewardItem : ItemEffect
{
    [Header("����Ч��")]
    public int extraCoins = 5;  // �������Ľ������

    private void OnEnable()
    {
        itemName = "�������";
        itemDescription = "��ɺ��ö�����";
        itemPrice = 5;
        isConsumable = true;  // ÿ��ʹ�ö���Ҫ����
        isPermanent = false;  // һ����Ч��
    }

    public override void OnPurchase()
    {
        Debug.Log($"������ {itemName}����ɺ󽫻�� {extraCoins} ��������");

        // ���ҽ�����������
        ProgressBarController progressBar = FindObjectOfType<ProgressBarController>();

        if (progressBar != null)
        {
            // ���ӵ���Ľ������
            progressBar.numberOfCoins += extraCoins;
            Debug.Log($"��ҵ����������ӵ�: {progressBar.numberOfCoins}");
        }
        else
        {
            // ����Ч��������ʹ��
            int currentExtra = PlayerPrefs.GetInt("ExtraRewardCoins", 0);
            PlayerPrefs.SetInt("ExtraRewardCoins", currentExtra + extraCoins);
            PlayerPrefs.Save();
        }
    }

    public override string GetDetailedDescription()
    {
        return $"��ɹؿ�������� {extraCoins} �����";
    }

    public override bool IsActive()
    {
        // ����һ���Ե��ߣ������������Ч
        return true;
    }
}