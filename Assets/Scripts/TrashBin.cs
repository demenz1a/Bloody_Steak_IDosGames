using UnityEngine;

/// <summary>
/// Мусорка. При взаимодействии предмет в руках игрока физически уничтожается.
/// Используется, чтобы избавиться от сгоревшего продукта (ошибка/потеря ресурса)
/// или от любого другого предмета, который стал не нужен.
/// </summary>
public class TrashBin : MonoBehaviour, IInteractable
{
    public bool CanInteract(PlayerInteractor player)
    {
        return player.Inventory.HasItem;
    }

    public void Interact(PlayerInteractor player)
    {
        var item = player.Inventory.TakeFromHands();
        if (item != null)
        {
            Destroy(item.gameObject);
        }
    }
}
