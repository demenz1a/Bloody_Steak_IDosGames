using UnityEngine;

public class OldPlayerKiller : MonoBehaviour
{
    private OldPanicZone nearbyClient;
    private OldMeatConverter nearbyMeatConverter;
    private OldKey nearbyKey;
    private Animator animator;
    public SpriteRenderer ClueSpriteRenderer;

    public AudioSource audios;
    public AudioClip KillClip;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (nearbyClient != null && !OldPlayerMovement.Instance.isHavingDeadBody)
        {
            if (OldKillerLockdown.IsLockdown && !OldBlackoutManager.IsBlackout)
            {
                OldKillerLockdown.ReportAttempt();
                return;
            }

            nearbyClient.Kill();
            OldKillCounter.Instance?.RegisterKill();
            audios.PlayOneShot(KillClip);
            animator.SetTrigger("Kill");
            OldPlayerMovement.Instance.isHavingDeadBody = true;
            return;

        }

        if (nearbyMeatConverter != null && OldPlayerMovement.Instance.isHavingDeadBody)
        {
            nearbyMeatConverter.Interact(this);
            OldPlayerMovement.Instance.isHavingDeadBody = false;
            return;
        }

        if (nearbyKey != null)
        {
            nearbyKey.Interact();
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out OldPanicZone npc))
            nearbyClient = npc;
        if (other.TryGetComponent(out OldMeatConverter meatConverter))
            nearbyMeatConverter = meatConverter;
        if (other.TryGetComponent(out OldKey key))
            nearbyKey = key;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<OldPanicZone>() == nearbyClient)
            nearbyClient = null;
        if (other.GetComponent<OldMeatConverter>() == nearbyMeatConverter)
            nearbyMeatConverter = null;
        if (other.GetComponent<OldKey>() == nearbyKey)
            nearbyKey = null;
    }
}