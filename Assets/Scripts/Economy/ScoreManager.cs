using System;
using UnityEngine;

/// <summary>
/// Очки за смену. Единственный источник правды по счёту.
///
/// Очки дают выполненные заказы (OrderManager.OnOrderCompleted) и убийства (NpcCustomer.Killed);
/// убийство паникующего клиента — в panicKillMultiplier раз дороже.
///
/// Конец смены:
/// - победа — остаток денег (MoneyManager.TakeAll) конвертируется в очки по moneyToScoreRate,
///   а заработанные деньги, делённые на moneyPerCoin (не больше maxCoinsPerShift), — в BloodyCoin;
/// - поражение — очки остаются как есть.
/// В обоих случаях итог отправляется в лидерборд iDosGames (IdosBridge.SubmitScore).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private GameOverManager gameOver;
    [SerializeField] private MoneyManager money;

    [Header("Points")]
    [SerializeField] private int pointsPerOrder = 100;
    [SerializeField] private int pointsPerKill = 50;
    [Tooltip("Во сколько раз больше очков за убийство паникующего клиента.")]
    [SerializeField] private int panicKillMultiplier = 2;

    [Header("End of shift")]
    [Tooltip("Сколько очков даёт одна монета при конвертации в конце смены.")]
    [SerializeField] private int moneyToScoreRate = 1;

    [Header("BloodyCoin за победу")]
    [Tooltip("Сколько денег смены дают 1 BloodyCoin: BloodyCoin = min(maxCoinsPerShift, деньги / moneyPerCoin).")]
    [SerializeField] private int moneyPerCoin = 10;
    [Tooltip("Больше этого за смену не выдаётся. Оболочка iDos дополнительно режет до 60 (номиналы 1..32).")]
    [SerializeField] private int maxCoinsPerShift = 60;

    public int Score { get; private set; }
    /// <summary>Сколько из итоговых очков пришло из конвертации денег (для экрана победы).</summary>
    public int ScoreFromMoney { get; private set; }
    /// <summary>Сколько BloodyCoin заработано за выигранную смену (для экрана победы).</summary>
    public int CoinsEarned { get; private set; }

    public event Action<int> OnScoreChanged;

    private void OnEnable()
    {
        orderManager.OnOrderCompleted += HandleOrderCompleted;
        NpcCustomer.Killed += HandleKilled;
        gameOver.OnVictory += HandleVictory;
        gameOver.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        orderManager.OnOrderCompleted -= HandleOrderCompleted;
        NpcCustomer.Killed -= HandleKilled;
        gameOver.OnVictory -= HandleVictory;
        gameOver.OnGameOver -= HandleGameOver;
    }

    private void HandleOrderCompleted(Order order) => Add(pointsPerOrder);

    private void HandleKilled(NpcCustomer victim, bool wasPanicking)
    {
        Add(wasPanicking ? pointsPerKill * panicKillMultiplier : pointsPerKill);
    }

    private void HandleVictory()
    {
        int earned = money != null ? money.TakeAll() : 0;
        ScoreFromMoney = earned * moneyToScoreRate;
        CoinsEarned = Mathf.Clamp(earned / Mathf.Max(1, moneyPerCoin), 0, maxCoinsPerShift);
        Add(ScoreFromMoney);
        IdosBridge.SubmitScore(Score);
        IdosBridge.ReportVictory(CoinsEarned); // BloodyCoin за смену начисляет оболочка iDos
    }

    private void HandleGameOver(GameOverReason reason) => IdosBridge.SubmitScore(Score);

    private void Add(int points)
    {
        if (points <= 0) return;

        Score += points;
        OnScoreChanged?.Invoke(Score);
    }
}
