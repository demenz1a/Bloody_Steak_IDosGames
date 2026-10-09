using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Иконки оставшихся попыток. Подписывается на OrderManager.OnAttemptsChanged — сам ничего не считает.
/// Иконка i видна, пока i меньше числа оставшихся попыток, поэтому пропадают они с конца массива.
/// Прячется именно компонент Image, а не GameObject, — так иконки могут быть вложены друг в друга.
/// </summary>
public class AttemptsUI : MonoBehaviour
{
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private Image[] icons;

    private void OnEnable()
    {
        orderManager.OnAttemptsChanged += HandleAttemptsChanged;
        HandleAttemptsChanged(orderManager.RemainingAttempts, orderManager.MaxAttempts);
    }

    private void OnDisable()
    {
        orderManager.OnAttemptsChanged -= HandleAttemptsChanged;
    }

    private void HandleAttemptsChanged(int remaining, int max)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] != null) icons[i].enabled = i < remaining;
        }
    }
}
