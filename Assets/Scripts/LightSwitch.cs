using System;
using UnityEngine;

/// <summary>
/// Рубильник: по Space вырубает свет в ресторане на blackoutDuration секунд. В темноте убивать
/// безопасно — свидетелей нет (PanicManager.ScanForWitnesses) и труп в руках никого не пугает
/// (NpcPanicZone). Убийство в темноте сокращает отключение: свет вернётся через afterKillDuration.
/// Пока темно, музыка молчит (MusicManager), экран затемняет BlackoutOverlayUI.
///
/// После использования рубильник перезаряжается cooldown секунд — прогресс показывает
/// радиальный таймер над ним (тот же CookingTimerUI, что у станций).
///
/// Состояние "свет выключен" статическое (LightsOut): в сцене один рубильник, а читают его
/// системы без ссылки на него. Сбрасывается в Awake, чтобы рестарт сцены посреди темноты
/// не оставил игру тёмной.
/// </summary>
public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Blackout")]
    [SerializeField] private float blackoutDuration = 5f;
    [Tooltip("Сколько ещё будет темно после убийства в темноте.")]
    [SerializeField] private float afterKillDuration = 1f;

    [Header("Cooldown")]
    [Tooltip("Перезарядка с момента использования, сек.")]
    [SerializeField] private float cooldown = 30f;
    [SerializeField] private CookingTimerUI cooldownUI;

    private float _blackoutLeft;
    private float _cooldownLeft;

    /// <summary>Свет сейчас выключен.</summary>
    public static bool LightsOut { get; private set; }

    /// <summary>true — свет выключили, false — включили.</summary>
    public static event Action<bool> LightsChanged;

    private void Awake()
    {
        LightsOut = false;
    }

    private void OnEnable() => NpcCustomer.Killed += HandleKilled;
    private void OnDisable() => NpcCustomer.Killed -= HandleKilled;

    private void OnDestroy()
    {
        if (LightsOut) SetLightsOut(false);
    }

    public bool CanInteract(PlayerInteractor player)
    {
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver) return false;
        return !LightsOut && _cooldownLeft <= 0f;
    }

    public void Interact(PlayerInteractor player)
    {
        _blackoutLeft = blackoutDuration;
        _cooldownLeft = cooldown;
        cooldownUI?.SetVisible(true);
        cooldownUI?.SetProgress(0f, isBurnWarning: false);

        SoundManager.Play(SoundId.LightSwitch);
        SetLightsOut(true);
    }

    private void HandleKilled(NpcCustomer victim, bool wasPanicking)
    {
        if (LightsOut) _blackoutLeft = Mathf.Min(_blackoutLeft, afterKillDuration);
    }

    private void Update()
    {
        // Конец игры замораживает время — без этого экран поражения/победы остался бы тёмным.
        if (LightsOut && GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver)
            SetLightsOut(false);

        if (LightsOut)
        {
            _blackoutLeft -= Time.deltaTime;
            if (_blackoutLeft <= 0f) SetLightsOut(false);
        }

        if (_cooldownLeft > 0f)
        {
            _cooldownLeft -= Time.deltaTime;
            cooldownUI?.SetProgress(cooldown > 0f ? 1f - _cooldownLeft / cooldown : 1f, isBurnWarning: false);
            if (_cooldownLeft <= 0f) cooldownUI?.SetVisible(false);
        }
    }

    private static void SetLightsOut(bool value)
    {
        if (LightsOut == value) return;

        LightsOut = value;
        LightsChanged?.Invoke(value);
    }
}
