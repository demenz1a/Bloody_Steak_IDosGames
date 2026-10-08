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
    [SerializeField] private float interactionDistance = 0.5f;
    [SerializeField] private LayerMask interactableMask;

    private IInteractable _currentTarget;
    private InteractionHighlightView _currentHighlight;

    public PlayerFormController Form => formController;
    public PlayerInventory Inventory => inventory;
    public PlayerCorpseState Corpse => corpseState;

    /// <summary>Вызывается NpcCustomer.Kill() в момент убийства — проигрывает анимацию удара у игрока.</summary>
    public void PlayKillAnimation()
    {
        if (animator != null) animator.SetTrigger(killTrigger);
    }

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
        Vector2 searchOrigin = (Vector2)transform.position + movement.FacingDirection * interactionDistance;
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchOrigin, interactionRadius, interactableMask);

        IInteractable best = null;
        float bestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;
            if (!interactable.CanInteract(this)) continue; // недоступно сейчас — не участвует в отборе

            float distSqr = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
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
        Vector2 dir = movement != null ? movement.FacingDirection : Vector2.down;
        Vector2 origin = (Vector2)transform.position + dir * interactionDistance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, interactionRadius);
    }
#endif
}
