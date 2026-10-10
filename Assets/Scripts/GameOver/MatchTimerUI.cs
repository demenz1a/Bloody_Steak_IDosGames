using TMPro;
using UnityEngine;

/// <summary>
/// Оставшееся время смены (м:сс). Только отображение — время считает MatchTimer.
/// В последние warningSeconds секунд текст перекрашивается в warningColor.
/// </summary>
public class MatchTimerUI : MonoBehaviour
{
    [SerializeField] private MatchTimer timer;
    [SerializeField] private TMP_Text label;

    [Header("Warning")]
    [SerializeField] private float warningSeconds = 30f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(0.91f, 0.14f, 0.24f);

    private int _shownSeconds = -1;

    private void Update()
    {
        int seconds = Mathf.CeilToInt(timer.Remaining);
        if (seconds == _shownSeconds) return;

        _shownSeconds = seconds;
        label.text = $"{seconds / 60}:{seconds % 60:00}";
        label.color = seconds <= warningSeconds ? warningColor : normalColor;
    }
}
