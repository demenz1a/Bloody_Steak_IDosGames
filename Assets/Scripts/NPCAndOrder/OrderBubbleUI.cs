using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Облачко над головой клиента: иконки блюд из его заказа (1 или 2) + радиальный таймер
/// ожидания. Показывается при создании Order, скрывается при полном выполнении или провале.
/// При частичной выдаче (заказ из 2 блюд) иконка доставленного блюда убирается через RefreshDishes.
/// </summary>
public class OrderBubbleUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Image[] dishIcons; // размер массива = максимум блюд в заказе (2)
    [SerializeField] private DishIconSet iconSet;

    public void Show(Order order)
    {
        if (root != null) root.SetActive(true);
        RefreshDishes(order);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    /// <summary>Перерисовать иконки под текущий список ЕЩЁ НЕ доставленных блюд заказа.</summary>
    public void RefreshDishes(Order order)
    {
        for (int i = 0; i < dishIcons.Length; i++)
        {
            bool hasDish = order != null && i < order.RemainingDishes.Count;

            dishIcons[i].enabled = hasDish;
            if (hasDish)
            {
                dishIcons[i].sprite = iconSet.GetSprite(order.RemainingDishes[i]);
            }
        }
    }

    public void SetTimerProgress(float remainingFraction)
    {
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = Mathf.Clamp01(remainingFraction);
        }
    }
}
