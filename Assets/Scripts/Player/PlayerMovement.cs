using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за перемещение игрока в 8 направлениях (+ анимацию ходьбы и флип спрайта
/// по направлению — это тоже часть "движения", а не отдельная система).
/// Ничего не знает про формы (овца/волк), готовку, заказы, взаимодействия и т.д.
/// Другие системы могут ЧИТАТЬ FacingDirection, но не могут дёргать движение напрямую —
/// это гарантирует, что готовка/ИИ НПС/переходы форм не смогут "сломать" управление.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParam = "Walking";
    [Tooltip("Транформ, который зеркалим по X в зависимости от направления движения. " +
             "Если персонаж составной (много частей тела) — это их общий родительский контейнер.")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private float _baseVisualScaleX;

    /// <summary>
    /// Последнее ненулевое направление движения (нормализованное).
    /// Используется системой взаимодействия (interaction zone), чтобы понимать,
    /// куда "смотрит" игрок.
    /// </summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    /// <summary>Двигается ли игрок прямо сейчас (для аниматора: Idle/Walk).</summary>
    public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;

    /// <summary>Заблокировано ли сейчас движение (например, во время анимации убийства).</summary>
    public bool IsMovementLocked { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;
        _rb.gravityScale = 0f;
        // Continuous нужен, чтобы на высокой скорости не проскакивать сквозь тонкие стены.
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (visualRoot != null)
        {
            _baseVisualScaleX = Mathf.Abs(visualRoot.localScale.x);
        }
    }

    private void Update()
    {
        if (!IsMovementLocked)
        {
            ReadInput();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (IsMovementLocked) return; // Rigidbody остаётся на месте — MovePosition просто не вызывается

        // MovePosition двигает Rigidbody2D с учётом физики.
        // Остановку у стен/мебели обеспечивают их Collider2D (не Trigger) —
        // это не логика этого скрипта, а обычная физика Unity.
        Vector2 targetPosition = _rb.position + _moveInput * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(targetPosition);
    }

    /// <summary>
    /// Включить/выключить блокировку движения. Сам PlayerMovement ничего не знает
    /// о том, ПОЧЕМУ движение блокируется — вызывать это должен внешний компонент
    /// (см. MovementLockEvents), реагирующий, например, на Animation Event.
    /// </summary>
    public void SetMovementLocked(bool isLocked)
    {
        IsMovementLocked = isLocked;
        if (isLocked) _moveInput = Vector2.zero; // сразу гасим остаточный ввод — не "доедем" по инерции кадра
    }

    private void ReadInput()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D или стрелки влево/вправо
        float y = Input.GetAxisRaw("Vertical");   // W/S или стрелки вверх/вниз

        _moveInput = new Vector2(x, y).normalized;

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            FacingDirection = _moveInput;
        }
    }

    /// <summary>Bool "Walking" в аниматоре + разворот спрайта по горизонтальной составляющей ввода.</summary>
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(walkingParam, IsMoving);
        }

        if (visualRoot != null && Mathf.Abs(_moveInput.x) > 0.01f)
        {
            float facingSign = _moveInput.x > 0f ? 1f : -1f;
            var scale = visualRoot.localScale;
            scale.x = _baseVisualScaleX * facingSign;
            visualRoot.localScale = scale;
        }
    }
}
