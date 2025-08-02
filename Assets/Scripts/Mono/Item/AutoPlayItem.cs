using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// �Զ�������� - ����ѡ��AD��JL�����������Զ����� (20���)
/// </summary>
[CreateAssetMenu(fileName = "Auto Play Item", menuName = "Shop/Items/Auto Play")]
public class AutoPlayItem : ItemEffect
{
    [Header("����Ч��")]
    [Range(0.8f, 1.0f)]
    public float autoPlayAccuracy = 0.95f;  // �Զ������׼ȷ��

    [Header("Ŀ��ѡ��")]
    public ControllerType targetController = ControllerType.Auto;

    public enum ControllerType
    {
        Auto,       // �Զ�ѡ�񣨻��ڱ��֣�
        AD,         // ָ��AD������
        JL,         // ָ��JL������
        Random      // ���ѡ��
    }

    [Header("�Զ�ѡ�����")]
    public AutoSelectStrategy autoSelectStrategy = AutoSelectStrategy.MostFails;

    public enum AutoSelectStrategy
    {
        MostFails,      // ʧ�ܴ�������
        LowestSuccess,  // �ɹ��������ٵ�
        LowestRatio     // �ɹ�����͵�
    }

    private void OnEnable()
    {
        itemName = "�Զ�����";
        itemDescription = "��һ����ɫ�Զ���ɰ���";
        itemPrice = 20;
        isConsumable = true;  // ���Զ�ι���
        isPermanent = true;
    }

    public override void OnPurchase()
    {
        Debug.Log($"������ {itemName}");

        // ��ȡĿ�������
        RhythmKeyControllerBase selectedController = GetTargetController();

        if (selectedController == null)
        {
            Debug.LogWarning("δ�ҵ����õĿ�������");
            // �˻����
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AddPlayerGold(itemPrice);
            }
            return;
        }

        // Ϊѡ�еĿ����������Զ�ģʽ
        selectedController.EnableAutoPlay(autoPlayAccuracy);

        // ����ÿ��������Զ�����״̬
        string prefKey = $"AutoPlay_{selectedController.keyConfigPrefix}";
        PlayerPrefs.SetInt(prefKey + "_Enabled", 1);
        PlayerPrefs.SetFloat(prefKey + "_Accuracy", autoPlayAccuracy);
        PlayerPrefs.Save();

        Debug.Log($"��Ϊ {selectedController.keyConfigPrefix} �����������Զ�����ģʽ");
    }

    private RhythmKeyControllerBase GetTargetController()
    {
        // �������п�����
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        // �������ÿ������б�
        List<RhythmKeyControllerBase> availableControllers = new List<RhythmKeyControllerBase>();

        // ���AD������
        if (adController != null && !IsAutoPlayEnabled(adController))
        {
            availableControllers.Add(adController);
        }

        // ���JL������
        if (jlController != null && !IsAutoPlayEnabled(jlController))
        {
            availableControllers.Add(jlController);
        }

        if (availableControllers.Count == 0)
        {
            Debug.LogWarning("û�п��õĿ����������������Զ�ģʽ��δ�ҵ���");
            return null;
        }

        // ����ѡ��ģʽ���ؿ�����
        switch (targetController)
        {
            case ControllerType.AD:
                return availableControllers.FirstOrDefault(c => c is ADController);

            case ControllerType.JL:
                return availableControllers.FirstOrDefault(c => c is JLController);

            case ControllerType.Random:
                return availableControllers[Random.Range(0, availableControllers.Count)];

            case ControllerType.Auto:
                return SelectByStrategy(availableControllers);

            default:
                return availableControllers[0];
        }
    }

    private RhythmKeyControllerBase SelectByStrategy(List<RhythmKeyControllerBase> controllers)
    {
        if (controllers.Count == 0) return null;
        if (controllers.Count == 1) return controllers[0];

        switch (autoSelectStrategy)
        {
            case AutoSelectStrategy.MostFails:
                return controllers.OrderByDescending(c => c.GetCurrentFailCount()).First();

            case AutoSelectStrategy.LowestSuccess:
                return controllers.OrderBy(c => c.successCount).First();

            case AutoSelectStrategy.LowestRatio:
                return controllers.OrderBy(c =>
                {
                    float total = c.successCount + c.GetCurrentFailCount();
                    return total > 0 ? c.successCount / total : 0f;
                }).First();

            default:
                return controllers[0];
        }
    }

    private bool IsAutoPlayEnabled(RhythmKeyControllerBase controller)
    {
        string prefKey = $"AutoPlay_{controller.keyConfigPrefix}_Enabled";
        return PlayerPrefs.GetInt(prefKey, 0) == 1;
    }

    public override string GetDetailedDescription()
    {
        string targetDesc = targetController switch
        {
            ControllerType.AD => "AD��������A/D����",
            ControllerType.JL => "JL��������J/L����",
            ControllerType.Random => "���һ��������",
            ControllerType.Auto => autoSelectStrategy switch
            {
                AutoSelectStrategy.MostFails => "ʧ�����Ŀ�����",
                AutoSelectStrategy.LowestSuccess => "�ɹ����ٵĿ�����",
                AutoSelectStrategy.LowestRatio => "�ɹ�����͵Ŀ�����",
                _ => "�Զ�ѡ��Ŀ�����"
            },
            _ => "һ��������"
        };

        return $"��{targetDesc}�Զ���ɰ�����׼ȷ�� {autoPlayAccuracy * 100}%";
    }

    public override bool CanPurchase()
    {
        if (!base.CanPurchase()) return false;

        // ����Ƿ���δ�����Զ�ģʽ�Ŀ�����
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        bool adAvailable = adController != null && !IsAutoPlayEnabled(adController);
        bool jlAvailable = jlController != null && !IsAutoPlayEnabled(jlController);

        return adAvailable || jlAvailable;
    }

    // ��ȡ��ǰ��Ϸ�����п�������״̬������UI��ʾ��
    public string GetControllersStatus()
    {
        ADController adController = FindObjectOfType<ADController>();
        JLController jlController = FindObjectOfType<JLController>();

        string status = "������״̬:\n";

        if (adController != null)
        {
            bool adAuto = IsAutoPlayEnabled(adController);
            status += $"AD (A/D��): {(adAuto ? "�Զ�" : "�ֶ�")} - ";
            status += $"�ɹ�{adController.successCount}�Σ�ʧ��{adController.GetCurrentFailCount()}��\n";
        }

        if (jlController != null)
        {
            bool jlAuto = IsAutoPlayEnabled(jlController);
            status += $"JL (J/L��): {(jlAuto ? "�Զ�" : "�ֶ�")} - ";
            status += $"�ɹ�{jlController.successCount}�Σ�ʧ��{jlController.GetCurrentFailCount()}��";
        }

        return status;
    }

    public override void ResetEffect()
    {
        // ��������Զ���������
        string[] prefixes = { "AD", "JL" };
        foreach (string prefix in prefixes)
        {
            string prefKey = $"AutoPlay_{prefix}";
            PlayerPrefs.DeleteKey(prefKey + "_Enabled");
            PlayerPrefs.DeleteKey(prefKey + "_Accuracy");
        }
        PlayerPrefs.Save();
    }
}