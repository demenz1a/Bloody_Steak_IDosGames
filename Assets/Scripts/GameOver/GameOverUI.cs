using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран геймовера. Только отображение — само состояние "игра окончена" живёт в GameOverManager.
///
/// Последовательность: экран сверху вниз заливается кровью (bloodOverlay), пауза,
/// затем плавно проявляется панель с надписью и кнопками "Начать заново" / "Выход в меню".
/// Всё анимируется на unscaled time, потому что GameOverManager может заморозить Time.timeScale.
///
/// Компонент должен висеть на ВСЕГДА активном объекте (иначе не получит событие),
/// а сам экран — в дочернем root, который выключен до геймовера.
///
/// Заливка сделана через anchorMin.y (1 -> 0) у bloodOverlay, поэтому работает с любым
/// спрайтом или вообще без него. bloodOverlay должен быть растянут на весь экран
/// (anchors 0..1) — скрипт меняет только нижний якорь.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameOverManager manager;

    [Header("Layout")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform bloodOverlay;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Text")]
    [SerializeField] private string titleText = "GAME OVER";

    [Header("Blood")]
    [SerializeField] private float bloodFillDuration = 1.6f;
    [SerializeField] private AnimationCurve bloodFillCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Panel")]
    [SerializeField] private float delayBeforePanel = 0.4f;
    [SerializeField] private float panelFadeDuration = 0.6f;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(manager.Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(manager.GoToMainMenu);
    }

    private void OnEnable() => manager.OnGameOver += HandleGameOver;
    private void OnDisable() => manager.OnGameOver -= HandleGameOver;

    private void HandleGameOver(GameOverReason reason)
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (root != null) root.SetActive(true);
        if (titleLabel != null) titleLabel.text = titleText;

        SetPanelVisible(0f, interactable: false);
        SetBloodFill(0f);

        for (float t = 0f; t < bloodFillDuration; t += Time.unscaledDeltaTime)
        {
            SetBloodFill(bloodFillCurve.Evaluate(t / bloodFillDuration));
            yield return null;
        }
        SetBloodFill(1f);

        yield return new WaitForSecondsRealtime(delayBeforePanel);

        for (float t = 0f; t < panelFadeDuration; t += Time.unscaledDeltaTime)
        {
            SetPanelVisible(t / panelFadeDuration, interactable: false);
            yield return null;
        }
        SetPanelVisible(1f, interactable: true);
    }

    private void SetBloodFill(float fill01)
    {
        if (bloodOverlay == null) return;

        var min = bloodOverlay.anchorMin;
        min.y = 1f - Mathf.Clamp01(fill01);
        bloodOverlay.anchorMin = min;
    }

    private void SetPanelVisible(float alpha, bool interactable)
    {
        if (panel == null) return;

        panel.alpha = alpha;
        panel.interactable = interactable;
        panel.blocksRaycasts = interactable;
    }
}
