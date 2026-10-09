using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Почему игра закончилась поражением.</summary>
public enum GameOverReason
{
    OutOfAttempts,  // закончились попытки — провалены заказы (см. OrderManager.OnGameOver)
    CaughtByPolice  // игрока-волка поймали (пока — сразу из NpcCustomer.CallPolice)
}

/// <summary>
/// Единственный источник правды по состоянию "игра окончена". Геймовер — это состояние
/// внутри GameScene, а не отдельная сцена: менеджер останавливает игру (музыка, управление,
/// время), а всё визуальное (кровь, надпись, кнопки) делает GameOverUI по событию OnGameOver.
///
/// Любая система, которая хочет закончить игру, вызывает TriggerGameOver(reason).
/// Повторные вызовы после первого игнорируются — геймовер срабатывает ровно один раз.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private OrderManager orderManager;

    [Header("Player")]
    [Tooltip("Отключается при геймовере, чтобы Space больше ничего не делал.")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio")]
    [Tooltip("Источники музыки, которые глушатся при геймовере. Музыки пока нет — можно оставить пустым.")]
    [SerializeField] private AudioSource[] musicSources;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip gameOverSound;

    [Header("Time")]
    [Tooltip("Останавливать ли игровое время (NPC, таймеры заказов, станции). UI геймовера работает на unscaled time.")]
    [SerializeField] private bool freezeTime = true;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool IsGameOver { get; private set; }
    public GameOverReason Reason { get; private set; }

    public event Action<GameOverReason> OnGameOver;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // страховка: после рестарта из замороженного геймовера время должно идти
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (orderManager != null) orderManager.OnGameOver += HandleOutOfAttempts;
    }

    private void OnDisable()
    {
        if (orderManager != null) orderManager.OnGameOver -= HandleOutOfAttempts;
    }

    private void HandleOutOfAttempts() => TriggerGameOver(GameOverReason.OutOfAttempts);

    public void TriggerGameOver(GameOverReason reason)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Reason = reason;

        StopMusic();
        if (sfxSource != null && gameOverSound != null) sfxSource.PlayOneShot(gameOverSound);

        if (playerInteractor != null) playerInteractor.enabled = false;
        if (playerMovement != null) playerMovement.SetMovementLocked(true);

        if (freezeTime) Time.timeScale = 0f;

        OnGameOver?.Invoke(reason);
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
