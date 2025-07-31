using UnityEngine;
using UnityEngine.UI; // 用于 Image 组件
using System.Collections; // 用于协程

public class ProgressBarController : MonoBehaviour
{
    public Image progressBarImage; // 拖拽你的 FanProgressBar Image 到这里
    public float fillDuration = 10f; // 进度条从0到100%填充所需的时间 (秒)

    private float timer = 0f;
    private bool isFilling = false;

    void Start()
    {
        // 确保进度条在开始时是空的
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("ProgressBar Image 未设置！请在 Inspector 中拖拽 FanProgressBar Image 组件。");
        }

        // 可以在游戏开始时立即启动填充，或者等待特定事件触发
        // StartFilling();
    }

    void Update()
    {
        // 示例：按空格键开始填充
        if (Input.GetKeyDown(KeyCode.Space) && !isFilling)
        {
            StartFilling();
        }
    }

    /// <summary>
    /// 开始填充进度条。
    /// </summary>
    public void StartFilling()
    {
        if (progressBarImage == null)
        {
            Debug.LogError("ProgressBar Image 为空，无法开始填充。");
            return;
        }

        if (isFilling)
        {
            Debug.LogWarning("进度条正在填充中，请勿重复启动。");
            return;
        }

        // 重置状态
        timer = 0f;
        progressBarImage.fillAmount = 0f;
        isFilling = true;

        // 启动协程来平滑填充
        StartCoroutine(FillProgressBarCoroutine());
    }

    /// <summary>
    /// 停止填充进度条（如果需要）。
    /// </summary>
    public void StopFilling()
    {
        if (isFilling)
        {
            StopAllCoroutines(); // 停止当前 GameObject 上的所有协程，包括填充协程
            isFilling = false;
            Debug.Log("进度条填充已停止。");
        }
    }

    /// <summary>
    /// 协程：在指定时间内填充进度条。
    /// </summary>
    IEnumerator FillProgressBarCoroutine()
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < fillDuration)
        {
            elapsedTime = Time.time - startTime;
            // 计算当前填充量，确保不会超过1
            progressBarImage.fillAmount = Mathf.Clamp01(elapsedTime / fillDuration);
            yield return null; // 等待下一帧
        }

        // 确保最终填充量为1 (100%)
        progressBarImage.fillAmount = 1f;
        isFilling = false;
        Debug.Log("进度条填充完成！");

        // TODO: 填充完成后可以触发其他事件，例如进入下一阶段、奖励玩家等
    }
}