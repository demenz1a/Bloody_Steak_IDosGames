using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран победы. Только отображение — состояние живёт в GameOverManager, очки в ScoreManager.
/// Устроен как GameOverUI, но вместо заливки кровью фон просто затемняется.
///
/// Последовательность: фон плавно темнеет (dimOverlay), пауза, проявляется панель с надписью,
/// разбивкой очков и кнопками "Начать заново" / "Выход в меню", итог "досчитывается" до конца.
/// Всё анимируется на unscaled time, потому что GameOverManager замораживает Time.timeScale.
///
/// Компонент должен висеть на ВСЕГДА активном объекте (иначе не получит событие),
/// а сам экран — в дочернем root, который выключен до победы.
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [SerializeField] private GameOverManager manager;
    [SerializeField] private ScoreManager score;

    [Header("Layout")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image dimOverlay;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text breakdownLabel;
    [SerializeField] private TMP_Text totalLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Text")]
    [SerializeField] private string titleText = "VICTORY";
    [Tooltip("{0} — очки за смену, {1} — очки из денег.")]
    [SerializeField] private string breakdownFormat = "SHIFT SCORE  {0}\nMONEY BONUS  +{1}";
    [SerializeField] private string totalFormat = "TOTAL  {0}";

    [Header("Dim")]
    [SerializeField] private float dimDuration = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float dimAlpha = 0.85f;

    [Header("Panel")]
    [SerializeField] private float delayBeforePanel = 0.3f;
    [SerializeField] private float panelFadeDuration = 0.5f;
    [SerializeField] private float totalCountDuration = 1f;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(manager.Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(manager.GoToMainMenu);
    }

    private void OnEnable() => manager.OnVictory += HandleVictory;
    private void OnDisable() => manager.OnVictory -= HandleVictory;

    private void HandleVictory() => StartCoroutine(PlaySequence());

    private IEnumerator PlaySequence()
    {
        if (root != null) root.SetActive(true);
        if (titleLabel != null) titleLabel.text = titleText;

        int total = score.Score;
        int fromMoney = score.ScoreFromMoney;
        if (breakdownLabel != null) breakdownLabel.text = string.Format(breakdownFormat, total - fromMoney, fromMoney);
        SetTotal(0);

        SetPanelVisible(0f, interactable: false);
        SetDim(0f);

        for (float t = 0f; t < dimDuration; t += Time.unscaledDeltaTime)
        {
            SetDim(t / dimDuration);
            yield return null;
        }
        SetDim(1f);

        yield return new WaitForSecondsRealtime(delayBeforePanel);

        for (float t = 0f; t < panelFadeDuration; t += Time.unscaledDeltaTime)
        {
            SetPanelVisible(t / panelFadeDuration, interactable: false);
            yield return null;
        }
        SetPanelVisible(1f, interactable: true);

        for (float t = 0f; t < totalCountDuration; t += Time.unscaledDeltaTime)
        {
            SetTotal(Mathf.RoundToInt(Mathf.Lerp(0f, total, t / totalCountDuration)));
            yield return null;
        }
        SetTotal(total);
    }

    private void SetDim(float k)
    {
        if (dimOverlay == null) return;

        var c = dimOverlay.color;
        c.a = Mathf.Clamp01(k) * dimAlpha;
        dimOverlay.color = c;
    }

    private void SetTotal(int value)
    {
        if (totalLabel != null) totalLabel.text = string.Format(totalFormat, value);
    }

    private void SetPanelVisible(float alpha, bool interactable)
    {
        if (panel == null) return;

        panel.alpha = alpha;
        panel.interactable = interactable;
        panel.blocksRaycasts = interactable;
    }
}
