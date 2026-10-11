using UnityEngine;

/// <summary>
/// Шаги игрока: пока PlayerMovement двигает персонажа, каждые stepInterval секунд звучит шаг.
/// Шаги по таймеру, а не Animation Events, потому что у формы повара и волка разные анимации
/// ходьбы; если захочется привязать шаг к кадру — выключите компонент и поставьте в анимации
/// событие AnimationSoundEvents.PlayFootstep.
/// </summary>
public class FootstepSounds : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private float stepInterval = 0.35f;

    private float _timer;

    private void Update()
    {
        bool walking = movement != null && movement.IsMoving && !movement.IsMovementLocked && Time.timeScale > 0f;
        if (!walking)
        {
            _timer = 0f; // первый шаг — сразу, как только тронулись
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = stepInterval;
        SoundManager.Play(SoundId.Footstep);
    }
}
