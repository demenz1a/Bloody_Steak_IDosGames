using UnityEngine;

/// <summary>
/// Состояние "игрок несёт труп". Труп — это реальный объект NpcCustomer в состоянии
/// NpcState.Dead, а не CarriableItem/Product (п. 13.4) — поэтому вместо HoldPoint
/// он просто становится ребёнком corpseHoldPoint на игроке.
///
/// Пока IsCarrying == true:
/// - PlayerInventory отказывает в попытке взять любой предмет;
/// - NpcCustomer отказывает в убийстве и в повторном подборе трупа;
/// - доступно только взаимодействие с MeatGrinderMachine (ConsumeCorpse).
/// </summary>
public class PlayerCorpseState : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorParam = "IsCarryingCorpse";
    [SerializeField] private Transform corpseHoldPoint;
    [SerializeField] private PlayerMovement movement;

    private NpcCustomer _carriedCorpse;

    public bool IsCarrying => _carriedCorpse != null;

    /// <summary>Вызывается NpcCustomer.PickUp(), когда игрок подбирает лежащий труп.</summary>
    public void PickUpCorpse(NpcCustomer corpse)
    {
        _carriedCorpse = corpse;

        corpse.transform.SetParent(corpseHoldPoint, worldPositionStays: false);
        corpse.transform.localPosition = Vector3.zero;
        corpse.OnPickedUp(movement); // чтобы труп мог разворачиваться вслед за игроком, пока его несут

        if (animator != null) animator.SetBool(animatorParam, true);
    }

    /// <summary>Вызывается мясорубкой: труп физически уничтожается, состояние сбрасывается.</summary>
    public void ConsumeCorpse()
    {
        if (_carriedCorpse != null)
        {
            Destroy(_carriedCorpse.gameObject);
            _carriedCorpse = null;
        }

        if (animator != null) animator.SetBool(animatorParam, false);
    }
}
