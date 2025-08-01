using UnityEngine;

public class ADController : RhythmKeyControllerBase
{
    void Reset()
    {
        keyConfig.primaryKey = KeyCode.A;
        keyConfig.secondaryKey = KeyCode.D;
        keyConfigPrefix = "AD";
    }
}
