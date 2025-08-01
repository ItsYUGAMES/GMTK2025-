using UnityEngine;
using UnityEngine.Events;

public class SingleKeyADAlternating : RhythmKeyControllerBase
{
    public UnityEvent onADKeyFailed;
    public UnityEvent onADKeySucceeded;
    public PauseManager pauseManager;

    void Reset()
    {
        keyConfig.primaryKey = KeyCode.A;
        keyConfig.secondaryKey = KeyCode.D;  // �_��D�IҲ���O��
        keyConfigPrefix = "AD";
    }

    // ֻ��A�I�|�l�ж�
    protected override void HandlePlayerInput()
    {
        if (isGameEnded) return;
        if (Input.GetKeyDown(keyConfig.primaryKey))
            OnKeyPressed(keyConfig.primaryKey);
    }

    // ���������I��������ҕ�X
    protected override void StartNextBeat()
    {
        if (pausedByManager) return;

        expectedKey = (beatCounter % 2 == 0) ? KeyCode.A : KeyCode.D;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        // ����expectedKey����������ҕ�XԪ��
        SetKeyColor(expectedKey, highlightKeyColor);
        beatCounter++;
    }

    // �،�OnKeyPressed��׌A�I��ƥ��A��D������
    protected override void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            if (pressedKey == keyConfig.primaryKey)
                ShowFeedback(keyConfig.primaryKey, missKeyColor);
            return;
        }

        // A�I�����|�l�ɹ��ж����oՓ������A߀��D��
        if (pressedKey == keyConfig.primaryKey)
            OnBeatSuccess();
    }

    protected override void OnBeatFailed()
    {
        base.OnBeatFailed();
        onADKeyFailed?.Invoke();
    }
}