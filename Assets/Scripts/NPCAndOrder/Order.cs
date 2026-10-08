using System.Collections.Generic;

/// <summary>Состояние конкретного заказа.</summary>
public enum OrderState
{
    Waiting,
    Completed,
    Failed
}

/// <summary>
/// Один активный заказ. Может состоять из 1 или 2 блюд (RequestedDishes).
/// RemainingDishes — те, что ещё не доставлены; заказ считается выполненным,
/// когда этот список опустеет. Не MonoBehaviour — просто объект данных.
/// </summary>
public class Order
{
    public NpcCustomer Customer { get; }
    public IReadOnlyList<DishFamily> RequestedDishes { get; }
    public float InitialTime { get; }
    public float RemainingTime { get; private set; }
    public OrderState State { get; private set; } = OrderState.Waiting;

    private readonly List<DishFamily> _remainingDishes;

    public IReadOnlyList<DishFamily> RemainingDishes => _remainingDishes;
    public bool IsFulfilled => _remainingDishes.Count == 0;

    public Order(NpcCustomer customer, IReadOnlyList<DishFamily> requestedDishes, float initialTime)
    {
        Customer = customer;
        RequestedDishes = requestedDishes;
        _remainingDishes = new List<DishFamily>(requestedDishes);
        InitialTime = initialTime;
        RemainingTime = initialTime;
    }

    public void Tick(float deltaTime) => RemainingTime -= deltaTime;

    public void ApplyPenalty(float amount) => RemainingTime -= amount;

    public void MarkCompleted() => State = OrderState.Completed;

    public void MarkFailed() => State = OrderState.Failed;

    /// <summary>
    /// Убрать одно доставленное блюдо из списка недостающих.
    /// Возвращает false, если такого блюда в заказе (ещё) не было — значит выдача ошибочная.
    /// </summary>
    public bool TryFulfillOne(DishFamily dish) => _remainingDishes.Remove(dish);
}
