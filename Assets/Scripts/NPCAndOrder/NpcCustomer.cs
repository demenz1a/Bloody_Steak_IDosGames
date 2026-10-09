using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ИИ одного клиента. Движение — через NavMeshAgent (совместимо и с обычным 3D NavMesh
/// с плоской геометрией, и с пакетом NavMeshPlus для честного 2D — API одинаковый).
///
/// Клиент делает заказ и ОСТАЁТСЯ на точке заказа (WaitingForFood) до тех пор, пока заказ
/// не будет полностью выполнен — точка заказа удерживается всё это время. К столику клиент
/// идёт только после этого.
///
/// После убийства (Kill) объект НЕ уничтожается — переходит в состояние Dead и лежит
/// на месте, пока игрок не подберёт труп отдельным взаимодействием (PickUp). После подбора
/// объект становится ребёнком игрока и уничтожается только в мясорубке
/// (см. PlayerCorpseState.ConsumeCorpse).
///
/// NPC сам НЕ создаёт Order — только просит об этом OrderManager (RequestOrder) и ждёт
/// коллбэк OnOrderCreated (п. 9.3).
///
/// NPC — сам IInteractable сразу для трёх взаимоисключающих действий: выдать заказ,
/// убить, подобрать труп. Взаимоисключение обеспечивается состояниями рук игрока и _state.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NpcCustomer : MonoBehaviour, IInteractable
{
    [Header("Timing")]
    [SerializeField] private float eatDuration = 8f;
    [SerializeField] private float toiletDuration = 4f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Navigation")]
    [Tooltip("Обходят ли клиенты друг друга (локальное избегание NavMeshAgent). Выключено намеренно: " +
             "с избеганием клиенты толпятся в узком входе и не пропускают друг друга к соседним " +
             "точкам заказа — стоящий клиент для агента тоже препятствие, и сосед никогда не доходит " +
             "до своей точки на arriveThreshold. Стены и путь по NavMesh от этого не зависят.")]
    [SerializeField] private bool avoidOtherCustomers = false;

    [Header("Toilet chance")]
    [Range(0f, 1f)]
    [SerializeField] private float toiletChance = 0.5f;

    [Header("Order bubble")]
    [SerializeField] private OrderBubbleUI orderBubble;

    [Header("Highlight")]
    [SerializeField] private InteractionHighlightView highlight;
    [SerializeField] private Color serveHighlightColor = Color.yellow;
    [SerializeField] private Color killHighlightColor = Color.red;
    [SerializeField] private Color corpseHighlightColor = Color.gray;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParam = "Walking";
    [SerializeField] private string deadTrigger = "Dead";
    [SerializeField] private string panicTrigger = "Panic";
    [Tooltip("Транформ, который зеркалим по X в зависимости от направления движения. " +
             "Если персонаж составной (много частей тела) — это их общий родительский контейнер.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float movementAnimThreshold = 0.05f;

    [Header("HeadSprite")]
    [SerializeField] private SpriteRenderer headSprite;
    //[SerializeField] private Sprite headSpriteDead;
    [SerializeField] private Sprite headSpriteAngry;
    //[SerializeField] private Sprite headSpritePanic;

    [Header("Panic")]
    [Tooltip("Радиус скана свидетелей в момент убийства (см. PanicManager.ScanForWitnesses).")]
    [SerializeField] private float killWitnessScanRadius = 4f;
    [Tooltip("Во сколько раз ускоряется NPC, убегая в панике.")]
    [SerializeField] private float panicSpeedMultiplier = 1.8f;

    [Header("Physics")]
    [Tooltip("Если на NPC есть Rigidbody2D — его нужно выключить при смерти, иначе Dynamic-тело " +
             "будет игнорировать родителя после SetParent (физика каждый FixedUpdate пересчитывает " +
             "позицию сама, независимо от Transform-иерархии). Можно оставить пустым, если Rigidbody2D нет.")]
    [SerializeField] private Rigidbody2D rb;

    private NavMeshAgent _agent;
    private OrderManager _orderManager;
    private PointGroup _tablePoints;
    private PointGroup _toiletPoints;
    private Transform _exitPoint;

    private NpcState _state;
    private ReservablePoint _reservedOrderPoint;
    private ReservablePoint _reservedTable;
    private ReservablePoint _reservedToilet;
    private Order _order;
    private float _stateTimer;
    private float _baseVisualScaleX;
    private float _baseAgentSpeed;
    private PlayerMovement _carriedByMovement;

    public IReadOnlyList<DishFamily> DesiredDishes { get; private set; }
    public bool IsPanicking => _state == NpcState.Panicking;
    public bool IsDead => _state == NpcState.Dead;
    /// <summary>Стоит на точке заказа и ждёт, пока OrderManager создаст ему Order.</summary>
    public bool IsWaitingForOrderCreation => _state == NpcState.WaitingForOrderCreation;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _baseAgentSpeed = _agent.speed;

        if (!avoidOtherCustomers)
        {
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        if (visualRoot != null)
        {
            _baseVisualScaleX = Mathf.Abs(visualRoot.localScale.x);
        }
    }

    /// <summary>
    /// Вызывается спаунером сразу после Instantiate. Точка заказа должна быть УЖЕ
    /// зарезервирована спаунером — это и есть проверка "все 3 точки заняты" из п. 9.5.
    /// </summary>
    public void Initialize(
        OrderManager orderManager,
        PointGroup tablePoints,
        PointGroup toiletPoints,
        Transform exitPoint,
        ReservablePoint reservedOrderPoint,
        IReadOnlyList<DishFamily> desiredDishes)
    {
        _orderManager = orderManager;
        _tablePoints = tablePoints;
        _toiletPoints = toiletPoints;
        _exitPoint = exitPoint;
        _reservedOrderPoint = reservedOrderPoint;
        DesiredDishes = desiredDishes;

        _state = NpcState.MovingToOrderPoint;
        _agent.SetDestination(reservedOrderPoint.transform.position);
    }

    private void Update()
    {
        if (_state == NpcState.Dead)
        {
            UpdateCorpseFacing(); // лежит на полу или его тащат — единственное, что ещё может меняться, это разворот
            return;
        }

        UpdateMovementAnimation();

        switch (_state)
        {
            case NpcState.MovingToOrderPoint:
                if (HasArrived())
                {
                    _state = NpcState.WaitingForOrderCreation;
                    _orderManager.RequestOrder(this);
                }
                break;

            case NpcState.WaitingForOrderCreation:
                // Ничего не делаем сами — ждём вызова OnOrderCreated() снаружи (от OrderManager).
                break;

            case NpcState.WaitingForFood:
                // Стоим на точке заказа. Обновляем облачко таймера, ждём Interact() от игрока
                // или провал заказа по таймеру (OnOrderFailed).
                if (_order != null)
                {
                    orderBubble?.SetTimerProgress(_order.RemainingTime / _order.InitialTime);
                }
                break;

            case NpcState.MovingToTable:
                if (HasArrived())
                {
                    _stateTimer = eatDuration;
                    _state = NpcState.Eating;
                }
                break;

            case NpcState.Eating:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) DecideAfterEating();
                break;

            case NpcState.MovingToToilet:
                if (HasArrived()) EnterAtToilet();
                break;

            case NpcState.AtToilet:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) EnterMovingToExit();
                break;

            case NpcState.MovingToExit:
                if (HasArrived()) Destroy(gameObject);
                break;

            case NpcState.Panicking:
                if (HasArrived())
                {
                    PanicManager.Instance.UnregisterPanic(this);
                    Destroy(gameObject);
                }
                break;
        }
    }

    /// <summary>
    /// Bool "Walking" в аниматоре + разворот спрайта по направлению фактической скорости
    /// NavMeshAgent. Работает от реальной скорости, а не от целевой точки — поэтому
    /// корректно отражает и разгон, и торможение при подходе к точке.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        Vector2 velocity = _agent.velocity;
        bool isMoving = velocity.sqrMagnitude > movementAnimThreshold * movementAnimThreshold;

        if (animator != null) animator.SetBool(walkingParam, isMoving);

        if (visualRoot != null && Mathf.Abs(velocity.x) > movementAnimThreshold)
        {
            float facingSign = velocity.x > 0f ? 1f : -1f;
            var scale = visualRoot.localScale;
            scale.x = _baseVisualScaleX * facingSign;
            visualRoot.localScale = scale;
        }
    }

    /// <summary>
    /// Вызывается PlayerCorpseState в момент подбора — чтобы труп мог визуально
    /// разворачиваться вслед за игроком, пока его тащат к мясорубке.
    /// </summary>
    public void OnPickedUp(PlayerMovement carrierMovement)
    {
        _carriedByMovement = carrierMovement;
    }

    /// <summary>
    /// Пока труп лежит на полу — ничего не делает (_carriedByMovement == null).
    /// Пока его несёт игрок — зеркалим visualRoot по горизонтальной составляющей
    /// его FacingDirection, тем же способом, каким NPC разворачивался при жизни.
    /// </summary>
    private void UpdateCorpseFacing()
    {
        if (_carriedByMovement == null || visualRoot == null) return;

        float facingX = _carriedByMovement.FacingDirection.x;
        if (Mathf.Abs(facingX) < 0.01f) return; // чисто вертикальное движение — сохраняем текущий разворот

        float facingSign = facingX > 0f ? 1f : -1f;
        var scale = visualRoot.localScale;
        scale.x = _baseVisualScaleX * facingSign;
        visualRoot.localScale = scale;
    }

    private bool HasArrived()
    {
        return !_agent.pathPending && _agent.remainingDistance <= arriveThreshold;
    }

    /// <summary>Вызывается OrderManager'ом, когда для этого клиента реально создан Order (п. 9.3).</summary>
    public void OnOrderCreated(Order order)
    {
        _order = order;
        _state = NpcState.WaitingForFood; // остаёмся на точке заказа, точка всё ещё зарезервирована
        orderBubble?.Show(order);
    }

    /// <summary>Вызывается OrderManager'ом, если заказ не выполнен вовремя (п. 7.3).</summary>
    public void OnOrderFailed()
    {
        _order = null;
        orderBubble?.Hide();
        EnterMovingToExit();
    }

    private void MoveToTableAfterOrderCompleted()
    {
        orderBubble?.Hide();
        ReleasePoint(ref _reservedOrderPoint);

        if (_tablePoints.TryReserveRandomFree(out _reservedTable))
        {
            _state = NpcState.MovingToTable;
            _agent.SetDestination(_reservedTable.transform.position);
        }
        else
        {
            // Столиков не нашлось — страховка, чтобы NPC не завис навечно.
            EnterMovingToExit();
        }
    }

    private void DecideAfterEating()
    {
        bool wantsToilet = Random.value < toiletChance;

        if (wantsToilet && _toiletPoints.TryReserveRandomFree(out _reservedToilet))
        {
            ReleasePoint(ref _reservedTable);
            _state = NpcState.MovingToToilet;
            _agent.SetDestination(_reservedToilet.transform.position);
        }
        else
        {
            EnterMovingToExit();
        }
    }

    private void EnterAtToilet()
    {
        _state = NpcState.AtToilet;
        _stateTimer = toiletDuration;
    }

    private void EnterMovingToExit()
    {
        ReleaseAllReservedPoints();

        _state = NpcState.MovingToExit;
        _agent.SetDestination(_exitPoint.position);
    }

    private void ReleaseAllReservedPoints()
    {
        ReleasePoint(ref _reservedOrderPoint);
        ReleasePoint(ref _reservedTable);
        ReleasePoint(ref _reservedToilet);
    }

    /// <summary>
    /// Освободить точку И забыть ссылку на неё. Обнулять ссылку обязательно: освобождённую
    /// точку сразу может занять другой NPC, и повторный Release() по старой ссылке
    /// (например, из ReleaseAllReservedPoints на выходе) освободил бы уже ЕГО точку —
    /// так на одной точке заказа оказывались два клиента.
    /// </summary>
    private static void ReleasePoint(ref ReservablePoint point)
    {
        if (point == null) return;

        point.Release();
        point = null;
    }

    // ---- IInteractable: три взаимоисключающих действия на одном объекте ----
    // Выдача заказа требует блюдо в руках. Убийство и подбор трупа требуют пустые руки,
    // но относятся к разным _state (alive vs Dead) — реального конфликта выбора действия нет.

    public bool CanInteract(PlayerInteractor player)
    {
        if (CanServeFood(player))
        {
            highlight?.SetColor(serveHighlightColor);
            return true;
        }

        if (CanBeKilled(player))
        {
            highlight?.SetColor(killHighlightColor);
            return true;
        }

        if (CanBePickedUp(player))
        {
            highlight?.SetColor(corpseHighlightColor);
            return true;
        }

        return false;
    }

    public void Interact(PlayerInteractor player)
    {
        if (CanServeFood(player)) DeliverFood(player);
        else if (CanBeKilled(player)) Kill(player);
        else if (CanBePickedUp(player)) PickUp(player);
    }

    // ---- Выдача заказа (п. 8.1/8.2) ----

    private bool CanServeFood(PlayerInteractor player)
    {
        return _state == NpcState.WaitingForFood
            && player.Inventory.HasItem
            && player.Inventory.CurrentProduct != null;
    }

    private void DeliverFood(PlayerInteractor player)
    {
        var product = player.Inventory.CurrentProduct;
        var result = _orderManager.TryDeliver(_order, product);

        if (result == DeliveryResult.Rejected)
        {
            return; // ничего не забираем, штраф по времени уже применён внутри TryDeliver
        }

        player.Inventory.TakeFromHands();
        Destroy(product.gameObject);

        if (result == DeliveryResult.OrderCompleted)
        {
            _order = null;
            MoveToTableAfterOrderCompleted();
        }
        else
        {
            // PartialSuccess: заказ из 2 блюд, одно доставлено — обновляем облачко и ждём второе.
            orderBubble?.RefreshDishes(_order);
        }
    }

    // ---- Убийство (п. 13.1-13.3) ----

    private bool CanBeKilled(PlayerInteractor player)
    {
        // Никаких дополнительных условий (п. 13.2) — только базовая физическая непротиворечивость:
        // форма волка, руки свободны, труп ещё не несём, сам NPC ещё жив.
        return _state != NpcState.Dead
            && player.Form.IsWolf
            && !player.Corpse.IsCarrying
            && !player.Inventory.HasItem;
    }

    private void Kill(PlayerInteractor player)
    {
        if (_order != null)
        {
            _orderManager.CancelOrder(_order); // без штрафа — это выбор игрока, а не провал по таймеру
            _order = null;
        }

        orderBubble?.Hide();
        ReleaseAllReservedPoints();
        //headSprite.sprite = headSpriteDead;
        player.PlayKillAnimation();
        if (animator != null) animator.SetTrigger(deadTrigger);

        // Труп дальше двигает только родитель (игрок после подбора) — агент больше не нужен
        // и его стоит отключить, чтобы он не пытался "поправить" позицию поверх переноса.
        _agent.enabled = false;

        // Критично: Dynamic Rigidbody2D игнорирует родительскую иерархию — физика пересчитывает
        // позицию сама каждый FixedUpdate, независимо от того, что стало родителем через SetParent.
        // Kinematic, в отличие от simulated=false, корректно следует за родителем И НЕ отключает
        // коллайдер от физических запросов (OverlapCircleAll всё ещё видит труп для подбора).
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        _state = NpcState.Dead;

        // Скан свидетелей: все NPC с зоной паники в радиусе мгновенно впадают в панику.
        PanicManager.Instance.ScanForWitnesses(transform.position, killWitnessScanRadius, this);
    }

    // ---- Паника (система паники: экспозиция трупа + скан свидетелей при убийстве) ----

    /// <summary>
    /// Немедленный переход в панику. Вызывается либо самой зоной паники этого NPC
    /// (экспозиция трупа набралась до конца — PanicSeverity.Normal), либо PanicManager'ом
    /// при скане свидетелей после чужого убийства (PanicSeverity.WitnessedKill).
    /// </summary>
    public void EnterPanic(PanicSeverity severity)
    {
        if (_state == NpcState.Dead || _state == NpcState.Panicking) return;

        if (_order != null)
        {
            _orderManager.CancelOrder(_order); // без штрафа — клиент в ужасе убежал, это не провал по таймеру
            _order = null;
        }

        orderBubble?.Hide();
        ReleaseAllReservedPoints();

        _agent.speed = _baseAgentSpeed * panicSpeedMultiplier;
        if (animator != null) animator.SetTrigger(panicTrigger);

        _state = NpcState.Panicking;
        _agent.SetDestination(_exitPoint.position);

        PanicManager.Instance.RegisterPanic(this, severity);

        if (severity == PanicSeverity.WitnessedKill)
        {
            CallPolice();
        }
    }

    /// <summary>
    /// Временно: свидетель убийства сразу заканчивает игру поражением.
    /// Позже здесь будет вызов полицейского, а геймовер — после его анимации.
    /// </summary>
    private void CallPolice()
    {
        GameOverManager.Instance?.TriggerGameOver(GameOverReason.CaughtByPolice);
    }

    // ---- Подбор трупа (п. 13.3, уточнение: теперь по отдельному нажатию, не автоматически) ----

    private bool CanBePickedUp(PlayerInteractor player)
    {
        return _state == NpcState.Dead
            && !player.Corpse.IsCarrying
            && !player.Inventory.HasItem;
    }

    private void PickUp(PlayerInteractor player)
    {
        player.Corpse.PickUpCorpse(this);
    }
}
