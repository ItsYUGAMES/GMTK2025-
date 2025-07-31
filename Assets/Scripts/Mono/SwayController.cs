using UnityEngine;

public class SwayController : MonoBehaviour
{
    [Header("摇摆设置")]
    public float swayAngle = 30f; // 摇摆角度（度）
    public float swaySpeed = 2f; // 摇摆速度
    public AnimationCurve swayCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 摇摆动画曲线

    private float currentSwayTime = 0f;

    private void Update()
    {
        UpdateSwayRotation();
    }

    private void UpdateSwayRotation()
    {
        // 更新摇摆时间
        currentSwayTime += Time.deltaTime * swaySpeed;

        // 计算摇摆角度，使用 Sin 函数实现连续的左右摇摆
        float swayProgress = Mathf.Sin(currentSwayTime);
        
        // 使用动画曲线让摇摆更自然
        float curveValue = swayCurve.Evaluate(Mathf.Abs(swayProgress));
        float finalAngle = Mathf.Sign(swayProgress) * swayAngle * curveValue;

        // 应用旋转
        transform.rotation = Quaternion.Euler(0, 0, finalAngle);
    }

    // 重置旋转
    public void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
        currentSwayTime = 0f;
    }

    // 设置摇摆速度
    public void SetSwaySpeed(float newSpeed)
    {
        swaySpeed = newSpeed;
    }

    // 设置摇摆角度
    public void SetSwayAngle(float newAngle)
    {
        swayAngle = newAngle;
    }

    // 获取当前摇摆角度
    public float GetCurrentSwayAngle()
    {
        return transform.rotation.eulerAngles.z;
    }

    // 显示摇摆范围的调试线
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        // 显示摇摆范围
        Gizmos.color = Color.yellow;
        
        // 左边界线
        Vector3 leftDirection = Quaternion.Euler(0, 0, swayAngle) * Vector3.up;
        Gizmos.DrawLine(center, center + leftDirection * 2f);
        
        // 右边界线
        Vector3 rightDirection = Quaternion.Euler(0, 0, -swayAngle) * Vector3.up;
        Gizmos.DrawLine(center, center + rightDirection * 2f);

        // 显示中心点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
}