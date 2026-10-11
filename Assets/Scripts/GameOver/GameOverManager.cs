using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Почему игра закончилась поражением.</summary>
public enum GameOverReason
{
    OutOfAttempts,  // закончились попытки — провалены заказы (см. OrderManager.OnGameOver)
    WitnessEscaped  // свидетель убийства добежал до выхода и поднял тревогу (NpcCustomer.EscapeRestaurant)
}

/// <summary>
/// Единственный источник правды по состоянию "игра окончена" — и поражением, и победой.
/// Конец игры — это состояние внутри GameScene, а не отдельная сцена: менеджер останавливает
/// игру (музыка, управление, время), а всё визуальное делают GameOverUI (по OnGameOver)
/// и VictoryUI (по OnVictory).
///
/// Поражение: любая система вызывает TriggerGameOver(reason).
/// Победа: игрок дожил до конца смены — MatchTimer.OnFinished вызывает TriggerVictory().
/// Повторные вызовы после первого игнорируются — игра заканчивается ровно один раз.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private OrderManager orderManager;
    [Tooltip("Таймер смены: по его окончании игрок побеждает. Можно не назначать — тогда победы нет.")]
    [SerializeField] private MatchTimer matchTimer;

    [Header("Player")]
    [Tooltip("Отключается в конце игры, чтобы Space больше ничего не делал.")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio")]
    [Tooltip("Источники музыки, которые глушатся в конце игры. Музыки пока нет — можно оставить пустым. " +
             "Звуки победы и поражения играет SoundManager по OnVictory / OnGameOver.")]
    [SerializeField] private AudioSource[] musicSources;

    [Header("Time")]
    [Tooltip("Останавливать ли игровое время (NPC, таймеры заказов, станции). UI конца игры работает на unscaled time.")]
    [SerializeField] private bool freezeTime = true;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// <summary>Игра закончилась — победой или поражением.</summary>
    public bool IsGameOver { get; private set; }
    public bool IsVictory { get; private set; }
    public GameOverReason Reason { get; private set; }

    public event Action<GameOverReason> OnGameOver;
    public event Action OnVictory;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // страховка: после рестарта из замороженного конца игры время должно идти
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (orderManager != null) orderManager.OnGameOver += HandleOutOfAttempts;
        if (matchTimer != null) matchTimer.OnFinished += TriggerVictory;
    }

    private void OnDisable()
    {
        if (orderManager != null) orderManager.OnGameOver -= HandleOutOfAttempts;
        if (matchTimer != null) matchTimer.OnFinished -= TriggerVictory;
    }

    private void HandleOutOfAttempts() => TriggerGameOver(GameOverReason.OutOfAttempts);

    public void TriggerGameOver(GameOverReason reason)
    {
        if (IsGameOver) return;

        Reason = reason;
        EndGame();
        OnGameOver?.Invoke(reason);
    }

    /// <summary>Игрок дожил до конца смены.</summary>
    public void TriggerVictory()
    {
        if (IsGameOver) return;

        IsVictory = true;
        EndGame();
        OnVictory?.Invoke();
    }

    private void EndGame()
    {
        IsGameOver = true;

        StopMusic();

        if (playerInteractor != null) playerInteractor.enabled = false;
        if (playerMovement != null) playerMovement.SetMovementLocked(true);

        if (freezeTime) Time.timeScale = 0f;
    }

    private void StopMusic()
    {
        if (musicSources == null) return;

        foreach (var source in musicSources)
        {
            if (source != null) source.Stop();
        }
    }

    /// <summary>Кнопка "Начать заново": перезагружает текущую сцену.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Кнопка "Выход в меню".</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
