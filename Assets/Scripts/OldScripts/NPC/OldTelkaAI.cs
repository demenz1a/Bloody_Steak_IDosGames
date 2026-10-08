using UnityEngine;

public class OldTelkaAI : OldNPCAI
{
    [Header("Telka Arrival FX")]
    public AudioSource sfxSource;
    public AudioClip arrivalClip;

    //[Header("Player Animator")]
    //public Animator playerAnimator;
    //public string playerTriggerName = "TelkaArrived";

    protected override void OnReachedOrderPoint()
    {
        // звук
        if (sfxSource != null && arrivalClip != null)
            sfxSource.PlayOneShot(arrivalClip);

        // триггер анимации на ГГ
        //if (playerAnimator != null && !string.IsNullOrEmpty(playerTriggerName))
        //    playerAnimator.SetTrigger(playerTriggerName);
    }
}
