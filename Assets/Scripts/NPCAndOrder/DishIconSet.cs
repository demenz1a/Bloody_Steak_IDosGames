using UnityEngine;

[System.Serializable]
public struct DishIconEntry
{
    public DishFamily dish;
    public Sprite icon;
}

/// <summary>
/// Маппинг DishFamily -> Sprite для отображения в облачке заказа.
/// Один общий ассет на все заказы, чтобы не дублировать спрайты в каждом префабе NPC.
/// </summary>
[CreateAssetMenu(menuName = "Restaurant/Dish Icon Set")]
public class DishIconSet : ScriptableObject
{
    [SerializeField] private DishIconEntry[] entries;

    public Sprite GetSprite(DishFamily dish)
    {
        foreach (var e in entries)
        {
            if (e.dish == dish) return e.icon;
        }
        return null;
    }
}
