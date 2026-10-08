using UnityEngine;

/// <summary>
/// Прокладка между Animation Event и PlayerMovement. Существует отдельным компонентом,
/// чтобы PlayerMovement оставался универсальным и не знал о конкретных анимациях
/// (убийство, любая другая future-анимация с обездвиживанием) — сюда можно вешать
/// Animation Event с ЛЮБОГО клипа, где нужно на время заблокировать движение игрока.
///
/// Вешается на тот же GameObject, где Animator — Animation Event ищет метод именно там.
/// </summary>
public class MovementLockEvents : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    /// <summary>Animation Event: движение блокируется.</summary>
    public void LockMovement()
    {
        movement.SetMovementLocked(true);
    }

    /// <summary>Animation Event: движение возвращается игроку.</summary>
    public void UnlockMovement()
    {
        movement.SetMovementLocked(false);
    }
}
