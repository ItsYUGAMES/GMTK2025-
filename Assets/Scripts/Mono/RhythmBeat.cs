using UnityEngine;

// [System.Serializable] 属性允许你在Inspector中编辑这个类的实例
[System.Serializable]
public class RhythmBeat
{
    public float time; // 这个节奏点应该出现的时间（相对于歌曲或序列开始）
    public KeyCode requiredKey; // 需要按下的键 (KeyCode.A 或 KeyCode.D)
    public bool isHit = false; // 玩家是否成功按下了这个节奏点
    public bool isProcessed = false; // 这个节奏点是否已经被处理过（例如，已经高亮或判定）
}