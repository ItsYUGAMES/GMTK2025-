using UnityEngine;

/// <summary>
/// ���ٻ���ֵ�����ܹ��ڣ����� (5���)
/// </summary>
[CreateAssetMenu(fileName = "Cheer Boost Item", menuName = "Shop/Items/Cheer Boost")]
public class CheerBoostItem : ItemEffect
{
    [Header("����Ч��")]
    [Range(0.5f, 0.9f)]
    public float progressMultiplier = 0.7f;  // ����������ٶȱ�����0.7 = ����30%ʱ�䣩

    private void OnEnable()
    {
        itemName = "���ܹ���";
        itemDescription = "���ٻ���ֵ����";
        itemPrice = 5;
        isConsumable = false;
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"������ {itemName}������ֵ�����ٶ�������");

        // ���ҽ�����������
        ProgressBarController progressBar = FindObjectOfType<ProgressBarController>();

        if (progressBar != null)
        {
            // �����������ʱ��
            progressBar.fillDuration *= progressMultiplier;
            Debug.Log($"���������ʱ������Ϊ: {progressBar.fillDuration} ��");

            // ���������������䣬���¿�ʼ��Ӧ�����ٶ�
            progressBar.StopFilling();
            progressBar.StartFilling();
        }
        else
        {
            // ����Ч��������ʹ��
            PlayerPrefs.SetFloat("CheerBoostMultiplier", progressMultiplier);
            PlayerPrefs.Save();
        }
    }

    public override string GetDetailedDescription()
    {
        int speedIncrease = Mathf.RoundToInt((1f / progressMultiplier - 1f) * 100f);
        return $"����ֵ�����ٶ����� {speedIncrease}%";
    }
}