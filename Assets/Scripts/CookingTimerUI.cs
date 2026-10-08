using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Радиальный индикатор прогресса готовки/сгорания над станцией.
/// Image должен быть настроен как Type = Filled, Fill Method = Radial 360.
/// Никакой игровой логики не содержит — только отображение того, что скажет CookingStation.
/// </summary>
public class CookingTimerUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Color cookingColor = Color.white;
    [SerializeField] private Color burnWarningColor = new Color(1f, 0.45f, 0.1f);

    private void Awake()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetProgress(float progress01, bool isBurnWarning)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = Mathf.Clamp01(progress01);
        fillImage.color = isBurnWarning ? burnWarningColor : cookingColor;
    }
}
