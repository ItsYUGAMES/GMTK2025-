using UnityEngine;

/// <summary>
/// ����Ч������ - ���е��߶��̳������������
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    [Header("���߻�����Ϣ")]
    public string itemName = "��������";
    public string itemDescription = "��������";
    public Sprite itemIcon;
    public int itemPrice = 10;

    [Header("��������")]
    public bool isConsumable = false;  // �Ƿ�Ϊ����Ʒ�����Զ�ι���
    public bool isPermanent = true;     // �Ƿ�Ϊ����Ч��

    /// <summary>
    /// ����ʱ����ִ�е�Ч��
    /// </summary>
    public abstract void OnPurchase();

    /// <summary>
    /// ��Ϸ��ʼʱִ�е�Ч������ѡ��
    /// </summary>
    public virtual void OnGameStart() { }

    /// <summary>
    /// ÿ�غϿ�ʼʱִ�е�Ч������ѡ��
    /// </summary>
    public virtual void OnRoundStart() { }

    /// <summary>
    /// ÿ�غϽ���ʱִ�е�Ч������ѡ��
    /// </summary>
    public virtual void OnRoundEnd() { }

    /// <summary>
    /// �������Ƿ���Թ��򣨿�����Ӷ���Ĺ���������
    /// </summary>
    /// <returns>�����Ƿ���Թ���</returns>
    public virtual bool CanPurchase()
    {
        // ������Ƿ��㹻
        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.GetPlayerGold() >= itemPrice;
        }
        return false;
    }

    /// <summary>
    /// ��ȡ���ߵ���ϸ���������԰�����ǰЧ��ֵ�ȶ�̬��Ϣ��
    /// </summary>
    /// <returns>������ϸ�����ı�</returns>
    public virtual string GetDetailedDescription()
    {
        return itemDescription;
    }

    /// <summary>
    /// ����Ч���Ƿ񼤻�
    /// </summary>
    /// <returns>���ص����Ƿ��ڼ���״̬</returns>
    public virtual bool IsActive()
    {
        return true;
    }

    /// <summary>
    /// ���õ���Ч������������Ϸ��ʼʱ��
    /// </summary>
    public virtual void ResetEffect()
    {
        // ���������д�˷����������ض���Ч��
    }
}