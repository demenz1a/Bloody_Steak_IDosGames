using System;
using UnityEngine;

/// <summary>
/// Ввод с сенсорного экрана для телефонов. Экранное управление (TouchControls) пишет сюда
/// направление джойстика и нажатие кнопки взаимодействия, а PlayerMovement и PlayerInteractor
/// читают их наравне с клавиатурой. На ПК здесь всегда ноль — управление как раньше.
///
/// Телефон определяется так: Application.isMobilePlatform (мобильный браузер в WebGL),
/// подсказка оболочки iDos (IdosBridge.OnHostSetMobile — "грубый" указатель, т.е. палец),
/// или первое касание экрана.
/// </summary>
public static class MobileInput
{
    private static bool _hostSaysMobile;
    private static bool _touchSeen;
    private static bool _interactPending;

    /// <summary>Направление джойстика, длина 0..1.</summary>
    public static Vector2 Move { get; private set; }

    public static bool IsMobile => Application.isMobilePlatform || _hostSaysMobile || _touchSeen;

    /// <summary>Изменилось IsMobile — TouchControls показывает или прячет себя.</summary>
    public static event Action MobileChanged;

    public static void SetHostMobile(bool mobile)
    {
        if (_hostSaysMobile == mobile) return;
        _hostSaysMobile = mobile;
        MobileChanged?.Invoke();
    }

    public static void NotifyTouch()
    {
        if (_touchSeen) return;
        _touchSeen = true;
        MobileChanged?.Invoke();
    }

    public static void SetMove(Vector2 move) => Move = Vector2.ClampMagnitude(move, 1f);

    public static void PressInteract() => _interactPending = true;

    /// <summary>
    /// Было ли нажатие кнопки взаимодействия с прошлой проверки. Флаг, а не "в этом кадре":
    /// TouchControls может обработать касание уже после Update игрока.
    /// </summary>
    public static bool ConsumeInteract()
    {
        bool pressed = _interactPending;
        _interactPending = false;
        return pressed;
    }

    /// <summary>Сброс при выключении управления (конец смены, перезагрузка сцены).</summary>
    public static void Clear()
    {
        Move = Vector2.zero;
        _interactPending = false;
    }
}
