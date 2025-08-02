using UnityEngine;

public class DestroyOnAnimEnd : MonoBehaviour
{
    // 这个函数会被动画事件调用
    public void DestroySelf()
    {
        // 销毁这个脚本所在的GameObject
        Destroy(gameObject);
    }
}