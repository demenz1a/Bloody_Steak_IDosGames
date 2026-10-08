using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоса подозрительности. "Целевое" значение читается из PanicManager каждый кадр (0..1),
/// но ОТОБРАЖАЕМОЕ значение плавно (MoveTowards) следует за целевым — поэтому мгновенный
/// скачок цели до 1 (например, при убийстве) визуально заполняется постепенно, а не рывком.
/// Белая при 0, краснее по мере роста; "сердцебиение" отсутствует при 0 и учащается/усиливается к 1.
/// </summary>
public class PanicBarUI : MonoBehaviour
{
    [SerializeField] private PanicManager panicManager;
    [SerializeField] private Image fillImage;
    [Tooltip("Что визуально 'бьётся' — обычно RectTransform самого fillImage или его родителя.")]
    [SerializeField] private RectTransform pulseTarget;

    [Header("Color")]
    [SerializeField] private Color emptyColor = Color.white;
    [SerializeField] private Color fullColor = Color.red;

    [Header("Heartbeat")]
    [SerializeField] private float minPulseSpeed = 0.5f;
    [SerializeField] private float maxPulseSpeed = 4f;
    [SerializeField] private float maxPulseScaleBoost = 0.15f; // насколько сильно "раздувается" при максимуме

    [Header("Smoothing")]
    [Tooltip("Скорость приближения отображаемого значения к целевому, в единицах шкалы (0..1) в секунду.")]
    [SerializeField] private float fillSpeed = 1.2f;

    private Vector3 _basePulseScale = Vector3.one;
    private float _displayedValue;

    private void Awake()
    {
        if (pulseTarget != null)
        {
            _basePulseScale = pulseTarget.localScale; // сохраняем твой ручной масштаб (например, растянутый под HUD)
        }
    }

    private void Update()
    {
        float target = panicManager.GetCurrentBarValue(); // уже нормализовано 0..1
        _displayedValue = Mathf.MoveTowards(_displayedValue, target, fillSpeed * Time.deltaTime);

        float t = _displayedValue;

        if (fillImage != null)
        {
            fillImage.fillAmount = t;
            fillImage.color = Color.Lerp(emptyColor, fullColor, t);
        }

        if (pulseTarget != null)
        {
            // При t=0 амплитуда нулевая — сердцебиения не видно вообще, как и требуется.
            float speed = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, t);
            float amplitude = maxPulseScaleBoost * t;
            float pulse = 1f + amplitude * (0.5f + 0.5f * Mathf.Sin(Time.time * speed * Mathf.PI * 2f));
            pulseTarget.localScale = _basePulseScale * pulse; // домножаем ИСХОДНЫЙ масштаб, а не заменяем его
        }
    }
}
