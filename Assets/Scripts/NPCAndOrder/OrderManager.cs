using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Результат попытки выдать блюдо клиенту.</summary>
public enum DeliveryResult
{
    Rejected,        // не подошло/сгорело — штраф по времени, ничего не забрано у клиента
    PartialSuccess,  // одно из 1-2 блюд заказа доставлено, заказ ещё не завершён
    OrderCompleted   // доставлены все блюда заказа
}

/// <summary>
/// Единственный источник правды по заказам во всей игре.
///
/// Два раздельных понятия:
/// - _pendingCustomers — очередь NPC, которые УЖЕ стоят на точке заказа и хотят заказать,
///   но реальный Order для них ещё не создан (либо все 3 слота заняты, либо игрок не в форме Sheep).
/// - _activeOrders — реальные заказы с таймером, максимум maxActiveOrders штук (п. 7.1/7.2).
///
/// Таймеры считаются с масштабом от текущей формы игрока (п. 7.4): на кухне x1,
/// в форме волка — сильно медленнее (wolfTimeScale), но не останавливаются полностью.
/// Переход в форму Sheep дополнительно пытается разгрести очередь ожидания (п. 9.3/9.5).
/// </summary>
public class OrderManager : MonoBehaviour
{
    [SerializeField] private PlayerFormController playerForm;

    [Header("Orders")]
    [SerializeField] private int maxActiveOrders = 3;
    [SerializeField] private float orderDuration = 30f;

    [Header("Time scale")]
    [SerializeField] private float wolfTimeScale = 0.35f;

    [Header("Penalties")]
    [SerializeField] private float wrongDeliveryPenalty = 5f;
    [SerializeField] private int maxMistakes = 5;

    private readonly List<Order> _activeOrders = new List<Order>();
    private readonly Queue<NpcCustomer> _pendingCustomers = new Queue<NpcCustomer>();

    public IReadOnlyList<Order> ActiveOrders => _activeOrders;
    public bool HasFreeSlot => _activeOrders.Count < maxActiveOrders;
    public int MistakeCount { get; private set; }

    public event Action<Order> OnOrderCreated;
    public event Action<Order> OnOrderCompleted;
    public event Action<Order> OnOrderFailed;
    public event Action OnGameOver;

    private void OnEnable() => playerForm.OnFormChanged += HandleFormChanged;
    private void OnDisable() => playerForm.OnFormChanged -= HandleFormChanged;

    private void HandleFormChanged(PlayerForm newForm)
    {
        // Возврат на кухню — шанс наконец создать заказы тем, кто уже стоит в очереди.
        if (newForm == PlayerForm.Sheep) TryDequeuePendingCustomers();
    }

    /// <summary>
    /// Вызывается NPC, когда он дошёл до точки заказа и готов сделать заказ (п. 9.3).
    /// Заказ создаётся НЕ сразу — только если есть свободный слот И игрок сейчас на кухне.
    /// </summary>
    public void RequestOrder(NpcCustomer customer)
    {
        _pendingCustomers.Enqueue(customer);

        if (playerForm.IsSheep) TryDequeuePendingCustomers();
    }

    private void TryDequeuePendingCustomers()
    {
        while (_pendingCustomers.Count > 0 && HasFreeSlot)
        {
            var customer = _pendingCustomers.Dequeue();
            if (customer == null) continue; // клиента убили, пока он ждал в очереди — пропускаем

            var order = new Order(customer, customer.DesiredDishes, orderDuration);

            _activeOrders.Add(order);
            customer.OnOrderCreated(order);
            OnOrderCreated?.Invoke(order);
        }
    }

    private void Update()
    {
        float scale = playerForm.IsWolf ? wolfTimeScale : 1f;

        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            var order = _activeOrders[i];
            order.Tick(Time.deltaTime * scale);

            if (order.RemainingTime <= 0f)
            {
                FailOrder(order);
            }
        }
    }

    /// <summary>
    /// Попытка выдать блюдо по заказу (п. 8.2/8.3). Вызывается из NpcCustomer.Interact().
    /// Заказ может требовать 1-2 блюда: верная выдача снимает ОДНО блюдо из списка недостающих,
    /// заказ считается выполненным только когда список опустеет. При неверном/сгоревшем блюде
    /// заказ НЕ проваливается сразу — просто штрафуется по времени.
    /// </summary>
    public DeliveryResult TryDeliver(Order order, Product product)
    {
        if (order == null || order.State != OrderState.Waiting) return DeliveryResult.Rejected;

        bool matches = product != null
            && !product.IsBurned
            && ProductFamily.TryGetFamily(product.Type, out var family)
            && order.TryFulfillOne(family);

        if (!matches)
        {
            order.ApplyPenalty(wrongDeliveryPenalty);
            return DeliveryResult.Rejected;
        }

        if (order.IsFulfilled)
        {
            CompleteOrder(order);
            return DeliveryResult.OrderCompleted;
        }

        return DeliveryResult.PartialSuccess;
    }

    private void CompleteOrder(Order order)
    {
        order.MarkCompleted();
        _activeOrders.Remove(order);
        OnOrderCompleted?.Invoke(order);

        TryDequeuePendingCustomers(); // освободился слот — сразу пробуем занять его из очереди
    }

    private void FailOrder(Order order)
    {
        order.MarkFailed();
        _activeOrders.Remove(order);
        order.Customer.OnOrderFailed();
        OnOrderFailed?.Invoke(order);

        MistakeCount++;
        if (MistakeCount >= maxMistakes)
        {
            OnGameOver?.Invoke();
        }

        TryDequeuePendingCustomers();
    }

    /// <summary>
    /// Убрать заказ БЕЗ штрафа-"ошибки" — используется, когда игрок убивает клиента
    /// до того, как заказ выполнен. Это осознанный игровой выбор, а не провал по таймеру.
    /// </summary>
    public void CancelOrder(Order order)
    {
        _activeOrders.Remove(order);
        TryDequeuePendingCustomers();
    }
}
