using UnityEngine;

public class OldBlackoutInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            OldBlackoutManager.Instance?.TriggerBlackout();
        }
    }

    public void BlackOut()
    {
        OldBlackoutManager.Instance?.TriggerBlackout();
    }
}
