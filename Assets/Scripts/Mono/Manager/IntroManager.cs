using System.Collections;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [Header("音频组件")]
    public AudioClip announcerAudio;
    public AudioClip audienceApplause;
    public AudioClip dragonMoveSFX; // 龙头移动音效
    
    [Header("龙头动画设置")]
    public Transform dragonHeadUp;
    public Transform dragonHeadDown;
    public float dragonMoveSpeed = 2f;
    public float dragonMoveDistance = 5f;

    [Header("龙头颤动设置")]
    public float tremorIntensity = 0.1f;     // 颤动强度
    public float tremorSpeed = 10f;          // 颤动速度
    public float tremorDuration = 2f;        // 颤动持续时间

    private Vector3 dragonUpStartPos;
    private Vector3 dragonUpTargetPos;
    private Vector3 dragonDownStartPos;
    private Vector3 dragonDownTargetPos;

    // 独立控制状态
    private bool isUpDragonMoving = false;
    private bool isDownDragonMoving = false;
    private bool isUpDragonTremoring = false;
    private bool isDownDragonTremoring = false;

    // 颤动基础位置存储
    private Vector3 upDragonBasePos;
    private Vector3 downDragonBasePos;

    // IntroManager应该只在Intro场景中存在，不需要单例
    void Awake()
    {
        // 移除DontDestroyOnLoad和单例逻辑
        // IntroManager只在当前场景中工作
    }
    void Start()
    {
        
        // 停止背景音乐[0]
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopSpecificBackgroundMusic(0);
            Debug.Log("进入Intro场景，停止背景音乐[0]");
        }
        // 自动查找龙头Transform
        FindDragonHeads();
    
        // 初始化上龙头位置
        if (dragonHeadUp != null)
        {
            dragonUpStartPos = dragonHeadUp.position;
            dragonUpTargetPos = dragonUpStartPos + Vector3.up * dragonMoveDistance;
            upDragonBasePos = dragonUpStartPos;
        }

        // 初始化下龙头位置
        if (dragonHeadDown != null)
        {
            dragonDownStartPos = dragonHeadDown.position;
            dragonDownTargetPos = dragonDownStartPos + Vector3.down * dragonMoveDistance;
            downDragonBasePos = dragonDownStartPos;
        }

        StartCoroutine(PlayCutscene());
    }

    private void FindDragonHeads()
    {
        // 通过名称查找龙头
        GameObject upDragon = GameObject.Find("Up");
        GameObject downDragon = GameObject.Find("Down");
    
        if (upDragon != null)
        {
            dragonHeadUp = upDragon.transform;
            Debug.Log("找到上龙头: " + upDragon.name);
        }
        else
        {
            Debug.LogError("未找到名为 'Up' 的龙头GameObject");
        }
    
        if (downDragon != null)
        {
            dragonHeadDown = downDragon.transform;
            Debug.Log("找到下龙头: " + downDragon.name);
        }
        else
        {
            Debug.LogError("未找到名为 'Down' 的龙头GameObject");
        }
    }

    IEnumerator PlayCutscene()
    {
        // 播放主持人的音频
        if (SFXManager.Instance != null && announcerAudio != null)
        {
            SFXManager.Instance.PlaySFX(announcerAudio);
            yield return new WaitForSeconds(announcerAudio.length);
        }

        // 同时开始龙头移动和播放掌声
        StartCoroutine(MoveBothDragonsWithApplause());

        // 等待龙头移动完成
        while (isUpDragonMoving || isDownDragonMoving)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);
        Debug.Log("剧情播放完成");
    }

    IEnumerator MoveBothDragonsWithApplause()
    {
        if (SFXManager.Instance != null && dragonMoveSFX != null)
        {
            AudioSource audioSource = SFXManager.Instance.GetAudioSource(); // 假设有方法获取 AudioSource
            audioSource.clip = dragonMoveSFX;
            audioSource.time = 0.1f;
            audioSource.Play();
        }

        // 播放掌声
        if (SFXManager.Instance != null && audienceApplause != null)
        {
            SFXManager.Instance.PlaySFX(audienceApplause);
        }

        // 开始龙头移动
        MoveBothDragons();

        yield return null;
    }

    #region 独立龙头移动控制

    /// <summary>
    /// 上龙头向上移动
    /// </summary>
    [ContextMenu("上龙头向上移动")]
    public void MoveUpDragonUp()
    {
        if (dragonHeadUp != null && !isUpDragonMoving)
        {
            StartCoroutine(AnimateUpDragon(dragonUpTargetPos));
        }
    }

    /// <summary>
    /// 上龙头回到初始位置
    /// </summary>
    [ContextMenu("上龙头回到初始位置")]
    public void ResetUpDragon()
    {
        if (dragonHeadUp != null && !isUpDragonMoving)
        {
            StartCoroutine(AnimateUpDragon(dragonUpStartPos));
        }
    }

    /// <summary>
    /// 下龙头向下移动
    /// </summary>
    [ContextMenu("下龙头向下移动")]
    public void MoveDownDragonDown()
    {
        if (dragonHeadDown != null && !isDownDragonMoving)
        {
            StartCoroutine(AnimateDownDragon(dragonDownTargetPos));
        }
    }

    /// <summary>
    /// 下龙头回到初始位置
    /// </summary>
    [ContextMenu("下龙头回到初始位置")]
    public void ResetDownDragon()
    {
        if (dragonHeadDown != null && !isDownDragonMoving)
        {
            StartCoroutine(AnimateDownDragon(dragonDownStartPos));
        }
    }

    private IEnumerator AnimateUpDragon(Vector3 targetPos)
    {
        isUpDragonMoving = true;
        Vector3 startPos = dragonHeadUp.position;
        float duration = Vector3.Distance(startPos, targetPos) / dragonMoveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // 计算当前应该在的基础位置
            Vector3 currentBasePos = Vector3.Lerp(startPos, targetPos, progress);
            upDragonBasePos = currentBasePos;

            // 无论是否颤动都设置位置，颤动会在下一帧覆盖
            dragonHeadUp.position = currentBasePos;

            yield return null;
        }

        upDragonBasePos = targetPos;
        dragonHeadUp.position = targetPos;
        isUpDragonMoving = false;
        Debug.Log("上龙头移动完成");
    }

    private IEnumerator AnimateDownDragon(Vector3 targetPos)
    {
        isDownDragonMoving = true;
        Vector3 startPos = dragonHeadDown.position;
        float duration = Vector3.Distance(startPos, targetPos) / dragonMoveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // 计算当前应该在的基础位置
            Vector3 currentBasePos = Vector3.Lerp(startPos, targetPos, progress);
            downDragonBasePos = currentBasePos;

            // 无论是否颤动都设置位置，颤动会在下一帧覆盖
            dragonHeadDown.position = currentBasePos;

            yield return null;
        }

        downDragonBasePos = targetPos;
        dragonHeadDown.position = targetPos;
        isDownDragonMoving = false;
        Debug.Log("下龙头移动完成");
    }

    #endregion

    #region 龙头颤动控制

    /// <summary>
    /// 上龙头开始颤动
    /// </summary>
    [ContextMenu("上龙头开始颤动")]
    public void StartUpDragonTremor()
    {
        if (dragonHeadUp != null && !isUpDragonTremoring)
        {
            upDragonBasePos = dragonHeadUp.position;
            StartCoroutine(UpDragonTremor());
        }
    }

    /// <summary>
    /// 下龙头开始颤动
    /// </summary>
    [ContextMenu("下龙头开始颤动")]
    public void StartDownDragonTremor()
    {
        if (dragonHeadDown != null && !isDownDragonTremoring)
        {
            downDragonBasePos = dragonHeadDown.position;
            StartCoroutine(DownDragonTremor());
        }
    }

    /// <summary>
    /// 停止上龙头颤动
    /// </summary>
    public void StopUpDragonTremor()
    {
        isUpDragonTremoring = false;
    }

    /// <summary>
    /// 停止下龙头颤动
    /// </summary>
    public void StopDownDragonTremor()
    {
        isDownDragonTremoring = false;
    }

    private IEnumerator UpDragonTremor()
    {
        isUpDragonTremoring = true;
        float elapsedTime = 0f;

        while (isUpDragonTremoring && elapsedTime < tremorDuration)
        {
            // 计算颤动偏移
            float offsetX = Mathf.Sin(Time.time * tremorSpeed) * tremorIntensity;
            float offsetY = Mathf.Cos(Time.time * tremorSpeed * 0.8f) * tremorIntensity;

            Vector3 tremorOffset = new Vector3(offsetX, offsetY, 0);

            // 使用当前的基础位置加上颤动偏移
            dragonHeadUp.position = upDragonBasePos + tremorOffset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 颤动结束后回到基础位置
        dragonHeadUp.position = upDragonBasePos;
        isUpDragonTremoring = false;
        Debug.Log("上龙头颤动结束");
    }

    private IEnumerator DownDragonTremor()
    {
        isDownDragonTremoring = true;
        float elapsedTime = 0f;

        while (isDownDragonTremoring && elapsedTime < tremorDuration)
        {
            // 计算颤动偏移
            float offsetX = Mathf.Sin(Time.time * tremorSpeed) * tremorIntensity;
            float offsetY = Mathf.Cos(Time.time * tremorSpeed * 0.8f) * tremorIntensity;

            Vector3 tremorOffset = new Vector3(offsetX, offsetY, 0);

            // 使用当前的基础位置加上颤动偏移
            dragonHeadDown.position = downDragonBasePos + tremorOffset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 颤动结束后回到基础位置
        dragonHeadDown.position = downDragonBasePos;
        isDownDragonTremoring = false;
        Debug.Log("下龙头颤动结束");
    }

    #endregion

    #region 组合操作

    /// <summary>
    /// 两个龙头同时移动
    /// </summary>
    [ContextMenu("两个龙头同时移动")]
    public void MoveBothDragons()
    {
        MoveUpDragonUp();
        MoveDownDragonDown();
    }

    /// <summary>
    /// 两个龙头同时颤动
    /// </summary>
    [ContextMenu("两个龙头同时颤动")]
    public void StartBothDragonsTremor()
    {
        StartUpDragonTremor();
        StartDownDragonTremor();
    }

    /// <summary>
    /// 重置所有龙头位置
    /// </summary>
    [ContextMenu("重置所有龙头位置")]
    public void ResetAllDragons()
    {
        ResetUpDragon();
        ResetDownDragon();
    }

    /// <summary>
    /// 停止所有龙头动画
    /// </summary>
    public void StopAllDragonAnimations()
    {
        StopUpDragonTremor();
        StopDownDragonTremor();
        StopAllCoroutines();
    }

    /// <summary>
    /// 重新播放剧情
    /// </summary>
    [ContextMenu("重新播放剧情")]
    public void ReplayCutscene()
    {
        StopAllDragonAnimations();
        ResetAllDragons();
        StartCoroutine(PlayCutscene());
    }

    #endregion
}
