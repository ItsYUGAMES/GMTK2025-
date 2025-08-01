using UnityEngine;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public bool IsPaused { get; private set; } = false;

    // 所有注册的可暂停脚本
    private readonly List<IPausable> pausableScripts = new List<IPausable>();

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

    // 注册
    public void Register(IPausable script)
    {
        if (!pausableScripts.Contains(script))
            pausableScripts.Add(script);
    }

    // 注销
    public void Unregister(IPausable script)
    {
        pausableScripts.Remove(script);
    }

    /// <summary>
    /// 全局暂停，exempt为豁免对象（只解锁这一个，其它都暂停）
    /// exempt=null时为全体暂停或全体恢复
    /// </summary>
    public void SetPause(bool pause, IPausable exempt = null)
    {
        IsPaused = pause;
        foreach (var script in pausableScripts)
        {
            if (!pause)
            {
                script.SetPaused(false); // 全体恢复
            }
            else
            {
                script.SetPaused(script != exempt); // 只豁免exempt
            }
        }
    }
}
