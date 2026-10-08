using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Набор точек одной категории — расставляются на сцене вручную, компонент просто
/// хранит на них ссылки и умеет находить/резервировать свободную. Используется
/// отдельным экземпляром для точек заказа, для столиков и для туалета.
/// </summary>
public class PointGroup : MonoBehaviour
{
    [SerializeField] private ReservablePoint[] points;

    public bool HasFree
    {
        get
        {
            foreach (var p in points)
            {
                if (!p.IsOccupied) return true;
            }
            return false;
        }
    }

    /// <summary>Первая свободная точка по порядку в массиве — для точек заказа (п. 9.5).</summary>
    public bool TryReserveFirstFree(out ReservablePoint point)
    {
        foreach (var p in points)
        {
            if (!p.IsOccupied)
            {
                p.Reserve();
                point = p;
                return true;
            }
        }

        point = null;
        return false;
    }

    /// <summary>Случайная свободная точка — для выбора столика (п. 9.5: "выбирают столик случайно").</summary>
    public bool TryReserveRandomFree(out ReservablePoint point)
    {
        var free = new List<ReservablePoint>();
        foreach (var p in points)
        {
            if (!p.IsOccupied) free.Add(p);
        }

        if (free.Count == 0)
        {
            point = null;
            return false;
        }

        point = free[Random.Range(0, free.Count)];
        point.Reserve();
        return true;
    }
}
