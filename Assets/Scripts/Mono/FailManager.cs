using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FailManager : MonoBehaviour
{
    [Header("移动设置")]
    public Transform upObject;
    public Transform downObject;
    public float moveSpeed = 2f;

    [Header("音效设置")]
    public AudioClip failSFX;
    public AudioClip doorCloseSFX;
    public float audioDelay = 1f;
    
    private bool isMoving = false;
    private bool hasExecuted = false; // 防止重复执行

    void Start()
    {
        // 只在Fail场景中执行
        if (SceneManager.GetActiveScene().name != "Fail")
        {
            Debug.Log("当前不是Fail场景，FailManager不执行移动逻辑");
            return;
        }

        // 防止重复执行
        if (hasExecuted) return;
        hasExecuted = true;

        // 自动查找Up和Down对象
        FindUpAndDownObjects();

        // 开始移动到(0,0,0)
        if (upObject != null && downObject != null)
        {
            StartCoroutine(MoveObjectsToOrigin());
        }
    }

    void OnEnable()
    {
        // 当脚本被重新启用时，检查场景
        if (hasExecuted && SceneManager.GetActiveScene().name != "Fail")
        {
            Debug.Log("FailManager在非Fail场景被启用，不执行移动逻辑");
        }
    }

    // 其余代码保持不变...
    public void RestartGame()
    {
        Debug.Log("按钮被点击了！");
        Debug.Log("重启游戏，返回GameStart场景");
        SceneManager.LoadScene("GameStart");
    }

    private void FindUpAndDownObjects()
    {
        if (upObject == null)
        {
            GameObject upObj = GameObject.Find("Up");
            if (upObj != null)
            {
                upObject = upObj.transform;
                Debug.Log("FailManager找到Up对象: " + upObj.name);
            }
        }

        if (downObject == null)
        {
            GameObject downObj = GameObject.Find("Down");
            if (downObj != null)
            {
                downObject = downObj.transform;
                Debug.Log("FailManager找到Down对象: " + downObj.name);
            }
        }

        if (upObject == null)
        {
            Debug.LogWarning("未找到Up对象，请确保场景中存在名为'Up'的GameObject");
        }

        if (downObject == null)
        {
            Debug.LogWarning("未找到Down对象，请确保场景中存在名为'Down'的GameObject");
        }
    }

    private IEnumerator MoveObjectsToOrigin()
    {
        if (isMoving) yield break;

        isMoving = true;
        Debug.Log("FailManager开始播放音效并移动Up和Down对象到世界坐标(0,0,0)");

        // 播放失败音效
        if (SFXManager.Instance != null && failSFX != null)
        {
            SFXManager.Instance.PlaySFX(failSFX);
            // 等待音效播放完成或指定时间
            yield return new WaitForSeconds(audioDelay);
        }

        Vector3 upStartPos = upObject.position;
        Vector3 downStartPos = downObject.position;
        Vector3 targetPos = Vector3.zero; // 世界坐标(0,0,0)

        float journey = 0f;

        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;

            upObject.position = Vector3.Lerp(upStartPos, targetPos, journey);
            downObject.position = Vector3.Lerp(downStartPos, targetPos, journey);

            yield return null;
        }

        upObject.position = targetPos;
        downObject.position = targetPos;

        Debug.Log("Up和Down对象已移动到世界坐标(0,0,0)");

        // 关门后播放第二个音效
        if (SFXManager.Instance != null && doorCloseSFX != null)
        {
            AudioSource close = SFXManager.Instance.GetAudioSource();
            close.clip = doorCloseSFX;
            close.time = 0.5f;
            SFXManager.Instance.PlaySFX(doorCloseSFX);
        }

        isMoving = false;
    }
}