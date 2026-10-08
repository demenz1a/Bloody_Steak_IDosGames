using System;
using UnityEngine;

/// <summary>
/// Мясной стол. Одновременно и физическое место, где лежит мясо (много HoldPoint'ов,
/// на каждом — реальный Product), и общий "запас" ресторана — отдельного MeatStock
/// больше нет: Current — это просто число занятых слотов, Max — их общее количество.
///
/// Игрок взаимодействует с самим столом (не с конкретным куском), стол сам решает,
/// какой из лежащих кусков отдать.
/// </summary>
public class MeatTable : MonoBehaviour, IInteractable
{
    [SerializeField] private HoldPoint[] slots;
    [SerializeField] private Product rawMeatPrefab;
    [SerializeField] private int startingAmount = 4;

    public int Max => slots.Length;
    public int Current { get; private set; }

    /// <summary>(current, max) — для UI-текста "Мясо: 4 / 10".</summary>
    public event Action<int, int> OnStockChanged;

    private void Start()
    {
        int toSpawn = Mathf.Clamp(startingAmount, 0, slots.Length);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnMeatInSlot(slots[i]);
        }

        Current = toSpawn;
        NotifyStockChanged();
    }

    public bool CanInteract(PlayerInteractor player)
    {
        return !player.Inventory.HasItem && Current > 0;
    }

    public void Interact(PlayerInteractor player)
    {
        var slot = FindOccupiedSlot();
        if (slot == null) return;

        var item = slot.TakeItem();

        if (player.Inventory.TryPickUp(item))
        {
            Current--;
            NotifyStockChanged();
        }
        else
        {
            slot.Place(item); // страховка: не удалось забрать — кладём обратно
        }
    }

    /// <summary>Вызывается мясоперерабатывающей машиной после переработки трупа.</summary>
    public void AddMeat(int amount)
    {
        int added = 0;

        for (int i = 0; i < amount; i++)
        {
            var emptySlot = FindEmptySlot();
            if (emptySlot == null) break; // стол переполнен — лишнее мясо теряется

            SpawnMeatInSlot(emptySlot);
            added++;
        }

        if (added > 0)
        {
            Current += added;
            NotifyStockChanged();
        }
    }

    private void SpawnMeatInSlot(HoldPoint slot)
    {
        var product = Instantiate(rawMeatPrefab, slot.transform.position, Quaternion.identity);
        slot.Place(product);
    }

    private HoldPoint FindOccupiedSlot()
    {
        foreach (var s in slots)
        {
            if (!s.IsEmpty) return s;
        }
        return null;
    }

    private HoldPoint FindEmptySlot()
    {
        foreach (var s in slots)
        {
            if (s.IsEmpty) return s;
        }
        return null;
    }

    private void NotifyStockChanged() => OnStockChanged?.Invoke(Current, Max);
}
