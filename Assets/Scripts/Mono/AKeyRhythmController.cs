using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ���I�����������ֻʹ��һ�����I�M�й������
/// </summary>
public class SingleKeyController : RhythmKeyControllerBase
{
    [Header("���I�O��")]
    public KeyCode singleKey = KeyCode.A;

    [Header("�¼�")]
    public UnityEvent onSingleKeyFailed;
    public UnityEvent onSingleKeySuccess;

    [Header("����������")]
    public PauseManager pauseManager;

    void Reset()
    {
        // �O�Þ�ͬһ�����I���@�ӹ�����ֻ��ʹ��һ���I
        keyConfig.primaryKey = KeyCode.Space;
        keyConfig.secondaryKey = KeyCode.Space;
        keyConfigPrefix = "SingleKey";

        // �{�����Iģʽ�ą���
        beatInterval = 1.0f;
        successWindow = 0.4f;
        successToPass = 15;  // ���I������Ҫ����ɹ��Δ�
        failToLose = 3;      // ʧ�����̶ȿ��Խ���
        needConsecutiveSuccessToResume = 3;
    }

    protected override void Awake()
    {
        // �_���ɂ��I���O�Þ�ͬһ���I
        keyConfig.primaryKey = singleKey;
        keyConfig.secondaryKey = singleKey;

        base.Awake();
    }

    protected override void Start()
    {
        // ֻ��Ҫһ���I��ҕ�X�������[�صڶ����I
        if (secondaryKeySpriteRenderer != null)
        {
            secondaryKeySpriteRenderer.gameObject.SetActive(false);
        }

        base.Start();
    }

    protected override void HandlePlayerInput()
    {
        if (isGameEnded) return;

        // ֻ�O ��һ���I
        if (Input.GetKeyDown(singleKey))
            OnKeyPressed(singleKey);
    }

    protected override void StartNextBeat()
    {
        if (pausedByManager) return;

        Debug.Log($"[{keyConfigPrefix}] 下一个节拍 - 按 {singleKey} 键");

        // 单键模式下，每个节拍都是同一个键
        expectedKey = singleKey;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        // 只设置主键sprite（不是颜色）
        SetKeySprite(singleKey, highlightKeySprite);
        beatCounter++;
    }

    protected override void OnBeatSuccess()
    {
        base.OnBeatSuccess();
        onSingleKeySuccess?.Invoke();

        // �z���Ƿ��_��ͨ�P�l��
        if (successCount >= successToPass)
        {
            isGameEnded = true;
            OnGameSuccess();
        }
    }

    protected override void OnBeatFailed()
    {
        // ����� PauseManager���ĕ�ͣ�б����Ƴ�
        if (pauseManager != null)
        {
            pauseManager.scriptsToPause.Remove(this);
        }

        base.OnBeatFailed();
        onSingleKeyFailed?.Invoke();
    }

    protected override void EnterPauseForFailure()
    {
        base.EnterPauseForFailure();
        Debug.Log($"[{keyConfigPrefix}] ���Iʧ����ͣ���B�m�� {singleKey} �I {needConsecutiveSuccessToResume} �λ֏�");
    }

    protected override void OnGameSuccess()
    {
        base.OnGameSuccess();
        Debug.Log($"[{keyConfigPrefix}] ���I����ͨ�P���ɹ��Δ�: {successCount}");
    }

    protected override void OnGameFail()
    {
        base.OnGameFail();
        Debug.Log($"[{keyConfigPrefix}] ���I����ʧ����ʧ���Δ�: {failCount}");
    }

    // �ṩ������ӑB���Ć��I
    public void SetSingleKey(KeyCode newKey)
    {
        singleKey = newKey;
        keyConfig.primaryKey = newKey;
        keyConfig.secondaryKey = newKey;
        expectedKey = newKey;
    }

    // ���Æ��I�O��
    public override void StartRhythm()
    {
        // �_���Iλ�O�����_
        keyConfig.primaryKey = singleKey;
        keyConfig.secondaryKey = singleKey;

        base.StartRhythm();
    }
}