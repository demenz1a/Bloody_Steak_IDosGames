using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Затемнение экрана, пока рубильник выключил свет (LightSwitch.LightsOut). Только отображение.
/// Лежит в GameUICanvas первым ребёнком: темнеет мир, а HUD (таймер, деньги, попытки) остаётся виден.
/// </summary>
public class BlackoutOverlayUI : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [Range(0f, 1f)]
    [SerializeField] private float darkAlpha = 0.92f;
    [SerializeField] private float fadeTime = 0.12f;

    private float _alpha;

    private void Awake() => Apply(0f);

    private void Update()
    {
        float target = LightSwitch.LightsOut ? darkAlpha : 0f;
        _alpha = Mathf.MoveTowards(_alpha, target, fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime * darkAlpha : 1f);
        Apply(_alpha);
    }

    private void Apply(float alpha)
    {
        _alpha = alpha;
        if (overlay == null) return;

        var c = overlay.color;
        c.a = alpha;
        overlay.color = c;
        overlay.enabled = alpha > 0.001f;
    }
}
