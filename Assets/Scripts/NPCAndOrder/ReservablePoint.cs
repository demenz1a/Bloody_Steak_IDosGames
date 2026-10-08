using UnityEngine;

/// <summary>
/// Одна точка на сцене (точка заказа, столик, место в туалете), которая может быть
/// свободна или занята. Ничего не знает о NPC — просто хранит статус занятости (п. 9.5).
/// </summary>
public class ReservablePoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    public void Reserve() => IsOccupied = true;
    public void Release() => IsOccupied = false;
}
