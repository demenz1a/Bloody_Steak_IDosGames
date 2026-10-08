using UnityEngine;

public class OldDeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (OldBlackoutManager.IsBlackout) return;

        if (other.TryGetComponent(out OldPanicZone npc))
        {
            npc.Panic();
            Debug.Log("jkdnfkj");
        }
    }
}
