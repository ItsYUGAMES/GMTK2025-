using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 切换到游戏场景
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene"); // 替换为你的游戏场景名称
    }
    
    /// <summary>
    /// 通过场景索引切换场景
    /// </summary>
    public void LoadGameSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
    
    /// <summary>
    /// 异步加载场景（推荐用于较大的场景）
    /// </summary>
    public void LoadGameSceneAsync()
    {
        StartCoroutine(LoadSceneAsync("GameScene"));
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            // 可以在这里显示加载进度
            Debug.Log($"加载进度: {asyncLoad.progress * 100}%");
            yield return null;
        }
    }
}