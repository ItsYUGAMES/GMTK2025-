using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RhythmControllerEntry
{
    public RhythmKeyControllerBase controller;
    public float startDelay = 0f;
}

public class GameManager : MonoBehaviour
{
    [Header("节奏控制器配置")]
    public List<RhythmControllerEntry> rhythmControllers;

    /// <summary>
    /// 切换到游戏场景
    /// </summary>
    public void GameStart()
    {
        SceneManager.LoadScene("GamePlay"); // 替换为你的游戏场景名称
    }

    // ... 原有的场景切换方法...

    public void StartAllRhythmControllers()
    {
        foreach (var entry in rhythmControllers)
        {
            StartCoroutine(StartControllerWithDelay(entry));
        }
    }

    private IEnumerator StartControllerWithDelay(RhythmControllerEntry entry)
    {
        yield return new WaitForSeconds(entry.startDelay);
        if (entry.controller != null)
        {
            entry.controller.StartRhythm();
            Debug.Log($"已启动控制器：{entry.controller.name}，延迟：{entry.startDelay}s");
        }
    }
}