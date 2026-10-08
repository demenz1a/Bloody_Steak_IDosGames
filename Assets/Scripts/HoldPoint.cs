using UnityEngine;

/// <summary>
/// Одна точка, в которой может физически находиться один CarriableItem.
/// Используется у MeatTable (много точек), CookingStation (одна точка),
/// PlayerInventory (одна точка — "руки").
///
/// Place() делает предмет ребёнком этой точки и ставит его в (0,0,0) локально —
/// поэтому позиция самого HoldPoint в сцене и определяет, где визуально лежит предмет.
/// Никакой физики: просто перепривязка Transform.
/// </summary>
public class HoldPoint : MonoBehaviour
{
    public CarriableItem CurrentItem { get; private set; }
    public bool IsEmpty => CurrentItem == null;

    public void Place(CarriableItem item)
    {
        CurrentItem = item;
        item.transform.SetParent(transform, worldPositionStays: false);
        item.transform.localPosition = Vector3.zero;
    }

    /// <summary>Забрать предмет из точки. Точка становится пустой. Сам предмет не уничтожается.</summary>
    public CarriableItem TakeItem()
    {
        var item = CurrentItem;
        CurrentItem = null;
        return item;
    }
}
