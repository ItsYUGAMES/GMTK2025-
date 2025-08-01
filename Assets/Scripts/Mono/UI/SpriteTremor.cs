using UnityEngine;
using System.Collections;

public class SpriteTremor : MonoBehaviour
{
    [Header("颤动类型")]
    public TremorType tremorType = TremorType.Vertical;
    public enum TremorType
    {
        Vertical,   // 上下颤动
        Horizontal, // 左右颤动
        Both        // 两个方向都颤动
    }

    [Header("颤动设置")]
    public float tremorHeight = 0.1f; // 上下颤动的最大高度
    public float tremorWidth = 0.1f;  // 左右颤动的最大宽度
    public float tremorDuration = 0.1f; // 单次颤动的持续时间
    public float tremorDelay = 0f; // 每次颤动之间的延迟

    [Header("强度递增设置")]
    public float intensityDuration = 10f; // 强度递增的总时长（秒）
    public float maxIntensityMultiplier = 5f; // 最大强度倍数

    private Vector3 initialPosition;
    private Coroutine tremorCoroutine;
    private float startTime; // 震动开始时间

    void Start()
    {
        initialPosition = transform.position;
        StartTremor();
    }

    public void StartTremor()
    {
        if (tremorCoroutine != null)
        {
            StopCoroutine(tremorCoroutine);
        }
        startTime = Time.time; // 记录开始时间
        tremorCoroutine = StartCoroutine(TremorRoutine());
    }

    public void StopTremor()
    {
        if (tremorCoroutine != null)
        {
            StopCoroutine(tremorCoroutine);
            tremorCoroutine = null;
        }
        transform.position = initialPosition;
    }

    // 动态切换颤动类型
    public void SetTremorType(TremorType newType)
    {
        tremorType = newType;
        // 重新开始颤动以应用新的类型
        StartTremor();
    }

    IEnumerator TremorRoutine()
    {
        while (true)
        {
            // 计算当前强度倍数
            float elapsedTime = Time.time - startTime;
            float intensityProgress = Mathf.Clamp01(elapsedTime / intensityDuration);
            float currentIntensity = Mathf.Lerp(1f, maxIntensityMultiplier, intensityProgress);

            switch (tremorType)
            {
                case TremorType.Vertical:
                    yield return StartCoroutine(VerticalTremor(currentIntensity));
                    break;
                case TremorType.Horizontal:
                    yield return StartCoroutine(HorizontalTremor(currentIntensity));
                    break;
                case TremorType.Both:
                    yield return StartCoroutine(BothDirectionTremor(currentIntensity));
                    break;
            }

            if (tremorDelay > 0)
            {
                yield return new WaitForSeconds(tremorDelay);
            }
        }
    }

    IEnumerator VerticalTremor(float intensity)
    {
        float currentHeight = tremorHeight * intensity;

        // 向上移动
        yield return MoveToTarget(initialPosition + Vector3.up * currentHeight, tremorDuration);
        // 向下移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
        // 向下移动 (低于初始位置)
        yield return MoveToTarget(initialPosition + Vector3.down * currentHeight, tremorDuration);
        // 再次向上移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
    }

    IEnumerator HorizontalTremor(float intensity)
    {
        float currentWidth = tremorWidth * intensity;

        // 向右移动
        yield return MoveToTarget(initialPosition + Vector3.right * currentWidth, tremorDuration);
        // 向左移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
        // 向左移动 (低于初始位置)
        yield return MoveToTarget(initialPosition + Vector3.left * currentWidth, tremorDuration);
        // 再次向右移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
    }

    IEnumerator BothDirectionTremor(float intensity)
    {
        float currentHeight = tremorHeight * intensity;
        float currentWidth = tremorWidth * intensity;

        // 向右上移动
        yield return MoveToTarget(initialPosition + Vector3.up * currentHeight + Vector3.right * currentWidth, tremorDuration);
        // 向左下移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
        // 向左上移动
        yield return MoveToTarget(initialPosition + Vector3.up * currentHeight + Vector3.left * currentWidth, tremorDuration);
        // 向右下移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
        // 向右下移动
        yield return MoveToTarget(initialPosition + Vector3.down * currentHeight + Vector3.right * currentWidth, tremorDuration);
        // 向左上移动 (回到初始位置)
        yield return MoveToTarget(initialPosition, tremorDuration);
        // 向左下移动
        yield return MoveToTarget(initialPosition + Vector3.down * currentHeight + Vector3.left * currentWidth, tremorDuration);
        // 回到初始位置
        yield return MoveToTarget(initialPosition, tremorDuration);
    }

    IEnumerator MoveToTarget(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }
}