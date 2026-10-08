using UnityEngine;
using TMPro;

/// <summary>
/// Текстовый индикатор запаса мяса вида "Мясо: 4 / 10".
/// Подписывается на MeatTable.OnStockChanged — сам ничего не считает.
/// </summary>
public class MeatStockUI : MonoBehaviour
{
    [SerializeField] private MeatTable meatTable;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "Мясо: {0} / {1}";

    private void OnEnable()
    {
        meatTable.OnStockChanged += HandleStockChanged;
        HandleStockChanged(meatTable.Current, meatTable.Max);
    }

    private void OnDisable()
    {
        meatTable.OnStockChanged -= HandleStockChanged;
    }

    private void HandleStockChanged(int current, int max)
    {
        label.text = string.Format(format, current, max);
    }
}
