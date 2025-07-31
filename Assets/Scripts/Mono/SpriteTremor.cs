using UnityEngine;
using System.Collections;

public class SpriteTremor : MonoBehaviour
{
    [Header("颤动设置")]
    public float tremorHeight = 0.1f; // 颤动的最大高度
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

    IEnumerator TremorRoutine()
    {
        while (true)
        {
            // 计算当前强度倍数
            float elapsedTime = Time.time - startTime;
            float intensityProgress = Mathf.Clamp01(elapsedTime / intensityDuration);
            float currentIntensity = Mathf.Lerp(1f, maxIntensityMultiplier, intensityProgress);
            
            // 计算当前震动高度
            float currentHeight = tremorHeight * currentIntensity;

            // 向上移动
            yield return MoveToTarget(initialPosition + Vector3.up * currentHeight, tremorDuration);

            // 向下移动 (回到初始位置)
            yield return MoveToTarget(initialPosition, tremorDuration);

            // 向下移动 (低于初始位置)
            yield return MoveToTarget(initialPosition + Vector3.down * currentHeight, tremorDuration);

            // 再次向上移动 (回到初始位置)
            yield return MoveToTarget(initialPosition, tremorDuration);

            if (tremorDelay > 0)
            {
                yield return new WaitForSeconds(tremorDelay);
            }
        }
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