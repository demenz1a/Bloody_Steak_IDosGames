using System;
using UnityEngine;

/// <summary>
/// Таймер смены: игра длится matchDuration секунд. Если игрок дожил до конца — победа
/// (GameOverManager подписан на OnFinished). Прогресс 0..1 читает DifficultyManager,
/// поэтому сложность растёт вместе с таймером и достигает максимума к концу смены.
///
/// Время считается по Time.deltaTime: при конце игры (timeScale = 0) таймер останавливается.
/// </summary>
public class MatchTimer : MonoBehaviour
{
    [Tooltip("Длительность смены в секундах.")]
    [SerializeField] private float matchDuration = 300f;

    private float _elapsed;

    public float Duration => matchDuration;
    public float Remaining => Mathf.Max(0f, matchDuration - _elapsed);
    /// <summary>Доля прошедшей смены 0..1.</summary>
    public float Progress01 => matchDuration > 0f ? Mathf.Clamp01(_elapsed / matchDuration) : 1f;
    public bool IsFinished { get; private set; }

    public event Action OnFinished;

    private void Update()
    {
        if (IsFinished) return;

        _elapsed += Time.deltaTime;
        if (_elapsed < matchDuration) return;

        _elapsed = matchDuration;
        IsFinished = true;
        OnFinished?.Invoke();
    }
}
