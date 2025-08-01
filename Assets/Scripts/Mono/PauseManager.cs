using UnityEngine;
using System;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public bool IsPaused { get; private set; } = false;

    public event Action<bool> OnPauseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPause(bool pause)
    {
        if (IsPaused == pause) return;
        IsPaused = pause;

        // 使用极慢时间而不是完全停止，这样视觉反馈仍然可以工作
        Time.timeScale = pause ? 0.1f : 1f;

        OnPauseChanged?.Invoke(pause);
    }

    private void OnDestroy()
    {
        // 确保销毁时恢复时间缩放
        Time.timeScale = 1f;
    }
}