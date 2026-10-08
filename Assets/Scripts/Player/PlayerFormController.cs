using System;
using UnityEngine;

/// <summary>
/// ≈диный источник правды о текущей форме игрока (Sheep/Wolf).
///  амера, система взаимодействий, UI и прочие системы подписываютс€
/// на OnFormChanged, а не хран€т собственную копию состо€ни€ формы.
///
/// —ама смена спрайта/скина Ч это анимационный переход внутри Animator Controller,
/// этот скрипт лишь выставл€ет bool-параметр, который запускает нужный переход.
/// </summary>
public class PlayerFormController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public PlayerForm CurrentForm { get; private set; } = PlayerForm.Sheep;

    /// <summary>¬ызываетс€ при каждой фактической смене формы (не вызываетс€ повторно на ту же форму).</summary>
    public event Action<PlayerForm> OnFormChanged;

    public void SetForm(PlayerForm newForm)
    {
        if (newForm == CurrentForm) return;

        CurrentForm = newForm;

        if (animator != null)
        {
            animator.SetTrigger("Swap");
        }

        OnFormChanged?.Invoke(newForm);
    }

    /// <summary>”добный шорткат дл€ систем взаимодействи€: доступны ли кухонные действи€ сейчас.</summary>
    public bool IsSheep => CurrentForm == PlayerForm.Sheep;

    /// <summary>”добный шорткат дл€ систем взаимодействи€: доступны ли волчьи действи€ сейчас.</summary>
    public bool IsWolf => CurrentForm == PlayerForm.Wolf;
}