using UnityEngine;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("将所有需要暂停的脚本拖拽到这个列表中。")]
    public List<MonoBehaviour> scriptsToPause = new List<MonoBehaviour>();

    // 追踪游戏是否处于暂停状态
    private bool isPaused = false;

    /// <summary>
    /// 暂停游戏，禁用所有列表中的脚本。
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return; // 如果已经暂停，则不再执行

        foreach (var script in scriptsToPause)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
        isPaused = true;
        Debug.Log("Game Paused");
    }

    /// <summary>
    /// 恢复游戏，启用所有列表中的脚本。
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return; // 如果没有暂停，则不再执行

        foreach (var script in scriptsToPause)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        isPaused = false;
        Debug.Log("Game Resumed");
    }

    // 示例：按下空格键来暂停/恢复游戏
    void Update()
    {
        
    }
}