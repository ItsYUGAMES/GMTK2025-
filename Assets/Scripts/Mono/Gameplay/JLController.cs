using UnityEngine;

public class JLController : RhythmKeyControllerBase
{
    void Reset()
    {
        keyConfig.primaryKey = KeyCode.J;
        keyConfig.secondaryKey = KeyCode.L;
        keyConfigPrefix = "JL";
    }
}
