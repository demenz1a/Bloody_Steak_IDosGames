using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Баланс денег в углу экрана. Только отображение — баланс хранит MoneyManager.
/// При начислении текст коротко "подпрыгивает" (на unscaled time).
/// </summary>
public class MoneyUI : MonoBehaviour
{
    [SerializeField] private MoneyManager money;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "$ {0}";

    [Header("Punch")]
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.25f;

    private Coroutine _punch;

    private void OnEnable()
    {
        money.OnBalanceChanged += HandleBalanceChanged;
        Show(money.Balance);
    }

    private void OnDisable()
    {
        money.OnBalanceChanged -= HandleBalanceChanged;
        label.transform.localScale = Vector3.one;
    }

    private void HandleBalanceChanged(int balance, int delta)
    {
        Show(balance);
        if (delta <= 0) return;

        if (_punch != null) StopCoroutine(_punch);
        _punch = StartCoroutine(Punch());
    }

    private void Show(int balance) => label.text = string.Format(format, balance);

    private IEnumerator Punch()
    {
        var t = label.transform;
        for (float time = 0f; time < punchDuration; time += Time.unscaledDeltaTime)
        {
            float k = Mathf.Sin(time / punchDuration * Mathf.PI);
            t.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, k);
            yield return null;
        }
        t.localScale = Vector3.one;
        _punch = null;
    }
}
