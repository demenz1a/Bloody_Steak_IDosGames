using UnityEngine;

/// <summary>
/// Инвентарь игрока: ровно один HoldPoint ("руки"). Вместо абстрактных данных
/// хранит реальный CarriableItem — при взятии предмет физически перепривязывается
/// к handPoint, при передаче станции/столу — к её HoldPoint. Сам инвентарь никакой
/// логики готовки не знает, только переносит ссылку.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private HoldPoint handPoint;
    [SerializeField] private PlayerCorpseState corpseState;

    public bool HasItem => !handPoint.IsEmpty;

    /// <summary>Текущий предмет в руках как есть (Product — мясо/блюдо). Труп сюда НЕ попадает — см. PlayerCorpseState.</summary>
    public CarriableItem CurrentItem => handPoint.CurrentItem;

    /// <summary>Удобный доступ, когда точно нужен именно Product (мясо/блюдо). Null, если несёшь что-то другое или ничего.</summary>
    public Product CurrentProduct => CurrentItem as Product;

    public bool TryPickUp(CarriableItem item)
    {
        if (HasItem) return false;
        if (corpseState != null && corpseState.IsCarrying) return false; // руки заняты трупом — п. 13.4

        handPoint.Place(item);
        return true;
    }

    /// <summary>Забрать предмет из рук (чтобы положить на станцию/стол/в машину). Null, если руки были пусты.</summary>
    public CarriableItem TakeFromHands()
    {
        return handPoint.TakeItem();
    }
}
