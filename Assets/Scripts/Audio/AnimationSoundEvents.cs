using System;
using UnityEngine;

/// <summary>
/// Приёмник Animation Events для звуков. Висит на объекте с Animator (игрок, клиент).
/// В событии анимации вызывайте PlaySound со строкой — именем SoundId (например "KillRaise"),
/// или готовые методы без параметров.
/// </summary>
public class AnimationSoundEvents : MonoBehaviour
{
    public void PlaySound(string soundId)
    {
        if (Enum.TryParse(soundId, out SoundId id)) SoundManager.Play(id);
        else Debug.LogWarning($"[AnimationSoundEvents] Неизвестный звук '{soundId}' в анимации на {name}.", this);
    }

    public void PlayKillRaise() => SoundManager.Play(SoundId.KillRaise);
    public void PlayBodyFall() => SoundManager.Play(SoundId.BodyFall);
    public void PlayFootstep() => SoundManager.Play(SoundId.Footstep);
}
