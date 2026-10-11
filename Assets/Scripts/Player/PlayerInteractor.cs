using UnityEngine;

/// <summary>
/// Единая точка входа для всех взаимодействий игрока (кнопка Space).
///
/// Каждый кадр ищет объекты вокруг игрока, оставляет только те, для которых
/// IInteractable.CanInteract() вернул true, и выбирает ближайший — это и есть
/// "единственное активное взаимодействие" из ТЗ. Если подходящих объектов нет,
/// currentTarget = null, и Space ничего не делает.
///
/// Благодаря тому что отбор идёт по CanInteract (а не просто по физическому пересечению),
/// подсветка гарантированно соответствует реальной доступности действия каждый кадр.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerFormController formController;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCorpseState corpseState;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string killTrigger = "Kill";

    [Header("Detection")]
    [SerializeField] private float interactionRadius = 0.6f;
    [Tooltip("Насколько круг поиска вынесен из середины тела в сторону взгляда.")]
    [SerializeField] private float interactionDistance = 0.5f;
    [SerializeField] private LayerMask interactableMask;

    [Header("Center")]
    [Tooltip("Середина тела, от которой строится круг поиска. Пусто — высота считается при старте " +
             "по спрайтам частей тела (bodyPartNames), по горизонтали — pivot игрока.")]
    [SerializeField] private Transform interactionCenter;
    [Tooltip("Части тела, по которым считается середина (хвост и спрайты перехода формы не учитываются).")]
    [SerializeField] private string[] bodyPartNames = { "Head", "Chest", "Pelvis", "LeftLeg", "RightLeg", "LeftFeet", "RightFeet" };

    private IInteractable _currentTarget;
    private InteractionHighlightView _currentHighlight;
    private float _centerHeight;
    private bool _centerComputed;

    /// <summary>
    /// Середина тела игрока. Pivot префаба не совпадает с телом (коллайдер ног ~1.6 юнита ниже),
    /// из-за чего круг поиска от pivot съезжал к ногам при взгляде вниз и к макушке при взгляде вверх.
    /// По горизонтали берётся pivot: визуал зеркалится вокруг него, и смещение по X менялось бы
    /// при каждом развороте.
    /// </summary>
    private Vector2 Center
    {
        get
        {
            if (interactionCenter != null) return interactionCenter.position;
            if (!_centerComputed) ComputeCenterHeight();
            return (Vector2)transform.position + new Vector2(0f, _centerHeight);
        }
    }

    private Vector2 SearchOrigin(Vector2 facing) => Center + facing * interactionDistance;

    private void ComputeCenterHeight()
    {
        _centerComputed = true;
        _centerHeight = 0f;

        bool any = false;
        Bounds bounds = default;
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (r.sprite == null || System.Array.IndexOf(bodyPartNames, r.name) < 0) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (any) _centerHeight = bounds.center.y - transform.position.y;
    }

    public PlayerFormController Form => formController;
    public PlayerInventory Inventory => inventory;
    public PlayerCorpseState Corpse => corpseState;

    /// <summary>Вызывается NpcCustomer.Kill() в момент убийства — проигрывает анимацию удара у игрока.</summary>
    public void PlayKillAnimation()
    {
        if (animator != null) animator.SetTrigger(killTrigger);
    }

    // Считаем середину сразу: позже к игроку цепляется труп, у которого части тела называются так же.
    private void Awake() => ComputeCenterHeight();

    private void Update()
    {
        DetectTarget();

        if (Input.GetKeyDown(KeyCode.Space) && _currentTarget != null)
        {
            _currentTarget.Interact(this);
            // Сразу пересчитываем цель — состояние объекта могло измениться
            // (например, станция из Empty перешла в Cooking и её тут же нельзя трогать).
            DetectTarget();
        }
    }

    private void DetectTarget()
    {
        Vector2 searchOrigin = SearchOrigin(movement.FacingDirection);
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchOrigin, interactionRadius, interactableMask);

        IInteractable best = null;
        float bestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;
            if (!interactable.CanInteract(this)) continue; // недоступно сейчас — не участвует в отборе

            // Ближайшая точка коллайдера к кругу, а не pivot объекта: у больших станций pivot далеко
            // от края, и раньше выигрывал не тот объект, к которому игрок реально стоит лицом.
            float distSqr = (hit.ClosestPoint(searchOrigin) - searchOrigin).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                best = interactable;
                bestDistSqr = distSqr;
            }
        }

        SetCurrentTarget(best);
    }

    private void SetCurrentTarget(IInteractable newTarget)
    {
        if (ReferenceEquals(newTarget, _currentTarget)) return;

        _currentHighlight?.SetActive(false);
        _currentTarget = newTarget;
        _currentHighlight = (_currentTarget as MonoBehaviour)?.GetComponent<InteractionHighlightView>();
        _currentHighlight?.SetActive(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) _centerComputed = false; // в редакторе спрайты могли поменяться
        Vector2 dir = movement != null ? movement.FacingDirection : Vector2.down;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Center, 0.1f); // середина тела
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SearchOrigin(dir), interactionRadius);
    }
#endif
}
