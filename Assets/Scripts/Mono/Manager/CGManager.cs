using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CGManager : MonoBehaviour
{
    [Header("移动设置")]
    public Transform spriteUp;        // 往上移动的sprite
    public Transform spriteDown;      // 往下移动的sprite
    public float moveSpeed = 2f;      // 移动速度
    public float moveDistance = 5f;   // 移动距离

    private Vector3 spriteUpStartPos;
    private Vector3 spriteDownStartPos;
    private Vector3 spriteUpTargetPos;
    private Vector3 spriteDownTargetPos;

    void Start()
    {
        if (spriteUp != null)
        {
            spriteUpStartPos = spriteUp.position;
            spriteUpTargetPos = spriteUpStartPos + Vector3.up * moveDistance;
        }

        if (spriteDown != null)
        {
            spriteDownStartPos = spriteDown.position;
            spriteDownTargetPos = spriteDownStartPos + Vector3.down * moveDistance;
        }

        StartMovement();
    }

    void Update()
    {
        // 检查游戏是否暂停
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsGamePaused())
            return;

        MoveSprites();
    }

    private void StartMovement()
    {
        // 开始移动动画
        if (spriteUp != null)
        {
            StartCoroutine(MoveSpriteToTarget(spriteUp, spriteUpTargetPos));
        }

        if (spriteDown != null)
        {
            StartCoroutine(MoveSpriteToTarget(spriteDown, spriteDownTargetPos));
        }
    }

    private void MoveSprites()
    {
        // 连续移动版本
        if (spriteUp != null)
        {
            spriteUp.Translate(Vector3.up * moveSpeed * Time.deltaTime);
        }

        if (spriteDown != null)
        {
            spriteDown.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
    }

    private IEnumerator MoveSpriteToTarget(Transform sprite, Vector3 targetPos)
    {
        Vector3 startPos = sprite.position;
        float duration = moveDistance / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // 检查游戏是否暂停
            if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsGamePaused())
            {
                yield return null;
                continue;
            }

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            sprite.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        sprite.position = targetPos;
    }

    // 重置位置
    public void ResetPositions()
    {
        if (spriteUp != null)
        {
            spriteUp.position = spriteUpStartPos;
        }

        if (spriteDown != null)
        {
            spriteDown.position = spriteDownStartPos;
        }
    }

    // 停止移动
    public void StopMovement()
    {
        StopAllCoroutines();
    }
}