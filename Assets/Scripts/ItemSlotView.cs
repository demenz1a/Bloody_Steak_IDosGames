using UnityEngine;

/// <summary>
/// Переиспользуемый компонент для отображения предмета в фиксированной точке объекта —
/// например "мясо лежит на столе", "блюдо готово на станции", "блюдо ждёт на стойке выдачи".
/// Как и у игрока, это просто SpriteRenderer без физики: показать/скрыть.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ItemSlotView : MonoBehaviour
{
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        //Hide();
    }

    public void Show(Sprite sprite)
    {
        _renderer.sprite = sprite;
        _renderer.enabled = true;
    }

    public void Hide()
    {
        _renderer.enabled = false;
    }
}