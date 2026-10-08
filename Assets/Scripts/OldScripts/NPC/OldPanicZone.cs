using UnityEngine;

public class OldPanicZone : MonoBehaviour
{
    private float suspicionTimer;
    private OldNPCAI npc;
    public bool isPanic;

    private void Awake()
    {
        npc = GetComponentInParent<OldNPCAI>();
    }

    private void Update()
    {
        if (OldBlackoutManager.IsBlackout)
        {
            isPanic = false;
            suspicionTimer = 0f;
            return;
        }

        if (isPanic)
        {
            suspicionTimer += Time.deltaTime;
            if (suspicionTimer >= 0.2f)
            {
                npc.Panic();
            }
        }
        else
        {
            suspicionTimer = 0f;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // � ������� NPC �� ��������
        if (OldBlackoutManager.IsBlackout)
        {
            isPanic = false;
            suspicionTimer = 0f;
            return;
        }

        if (OldPlayerMovement.Instance.isHavingDeadBody)
        {
            isPanic = true;
        }
        else
        {
            isPanic = false;
        }
    }


    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPanic = false;
        suspicionTimer = 0f;
    }

    public void Kill()
    {
        npc.DoKill();
    }

    public void Panic()
    {
        npc.Panic();
    }
}