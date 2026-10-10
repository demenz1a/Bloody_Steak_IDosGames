using System;
using UnityEngine;

/// <summary>
/// Деньги игрока за смену. Единственный источник правды по балансу.
///
/// За каждый выполненный заказ (OrderManager.OnOrderCompleted) начисляется случайная сумма.
/// Диапазон растёт со сложностью (DifficultyManager.NextOrderReward), а без DifficultyManager
/// берётся из инспектора. За проваленный заказ деньги не отнимаются.
///
/// Потратить деньги пока нельзя: в конце смены при победе остаток забирает ScoreManager
/// (TakeAll) и превращает в очки.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    [SerializeField] private OrderManager orderManager;
    [Tooltip("Если назначен — награда за заказ берётся из него (растёт со сложностью).")]
    [SerializeField] private DifficultyManager difficulty;

    [Header("Order reward (без DifficultyManager)")]
    [SerializeField] private int minOrderReward = 10;
    [SerializeField] private int maxOrderReward = 25;

    public int Balance { get; private set; }

    /// <summary>(новый баланс, изменение) — для MoneyUI.</summary>
    public event Action<int, int> OnBalanceChanged;

    private void OnEnable() => orderManager.OnOrderCompleted += HandleOrderCompleted;
    private void OnDisable() => orderManager.OnOrderCompleted -= HandleOrderCompleted;

    private void HandleOrderCompleted(Order order)
    {
        int reward = difficulty != null
            ? difficulty.NextOrderReward()
            : UnityEngine.Random.Range(minOrderReward, Mathf.Max(minOrderReward, maxOrderReward) + 1);
        Add(reward);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        Balance += amount;
        OnBalanceChanged?.Invoke(Balance, amount);
    }

    /// <summary>Забрать весь остаток (конвертация в очки в конце смены).</summary>
    public int TakeAll()
    {
        int amount = Balance;
        if (amount == 0) return 0;

        Balance = 0;
        OnBalanceChanged?.Invoke(Balance, -amount);
        return amount;
    }
}
