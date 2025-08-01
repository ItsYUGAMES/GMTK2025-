using System.Collections;
using UnityEngine;

public class IntroCutsceneManager : MonoBehaviour
{
    [Header("音频组件")]
    public AudioSource announcerAudio;
    public AudioSource audienceApplause;
    public AudioSource audienceLine;

    [Header("龙头动画设置")]
    public Transform dragonHead;
    public float dragonMoveSpeed = 2f;      // 龙头移动速度
    public float dragonMoveDistance = 5f;   // 龙头移动距离
    public bool moveDragonUp = true;        // 龙头移动方向（true=向上，false=向下）

    private Vector3 dragonStartPos;
    private Vector3 dragonTargetPos;

    void Start()
    {
        // 初始化龙头位置
        if (dragonHead != null)
        {
            dragonStartPos = dragonHead.position;
            Vector3 moveDirection = moveDragonUp ? Vector3.up : Vector3.down;
            dragonTargetPos = dragonStartPos + moveDirection * dragonMoveDistance;
        }

        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        // 播放主持人的音频
        if (announcerAudio != null)
        {
            announcerAudio.Play();
            yield return new WaitForSeconds(announcerAudio.clip.length);
        }

        // 播放掌声
        if (audienceApplause != null)
        {
            audienceApplause.Play();
            yield return new WaitForSeconds(audienceApplause.clip.length);
        }

        // 播放观众台词
        if (audienceLine != null)
        {
            audienceLine.Play();
            yield return new WaitForSeconds(audienceLine.clip.length);
        }

        // 开始龙头动画
        if (dragonHead != null)
        {
            yield return StartCoroutine(AnimateDragonHead());
        }

        yield return new WaitForSeconds(1.0f); // 动画完成后等待1秒
    }

    private IEnumerator AnimateDragonHead()
    {
        Vector3 startPos = dragonHead.position;
        float duration = dragonMoveDistance / dragonMoveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // 检查游戏是否暂停
            if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsGamePaused())
            {
                yield return null;
                continue;
            }

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            dragonHead.position = Vector3.Lerp(startPos, dragonTargetPos, progress);
            yield return null;
        }

        // 确保最终位置精确
        dragonHead.position = dragonTargetPos;
        Debug.Log("龙头动画完成");
    }

    // 重置龙头位置
    public void ResetDragonPosition()
    {
        if (dragonHead != null)
        {
            dragonHead.position = dragonStartPos;
        }
    }

    // 手动触发龙头动画
    public void TriggerDragonAnimation()
    {
        if (dragonHead != null)
        {
            StartCoroutine(AnimateDragonHead());
        }
    }

    // 停止所有动画
    public void StopAllAnimations()
    {
        StopAllCoroutines();
    }
}