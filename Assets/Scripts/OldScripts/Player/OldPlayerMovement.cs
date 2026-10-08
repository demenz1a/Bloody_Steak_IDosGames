using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class OldPlayerMovement : MonoBehaviour
{
    public float speed = 4f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private OldPlayerPickUp playerPickUp;
    private OldPlayerKiller playerKiller;
    public bool isKiller = false;
    public bool isHavingDeadBody;
    public static OldPlayerMovement Instance;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public static event System.Action<bool> OnKillerStateChanged;

    private Coroutine swapTimerCoroutine;
    public float swapDuration = 20f;
    //public GameObject waitingZone;

    void Awake()
    {
        Instance = this;
        playerPickUp = GetComponent<OldPlayerPickUp>();
        playerKiller = GetComponent<OldPlayerKiller>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");

        if (movement.x > 0.01f)
            spriteRenderer.flipX = true;  
        else if (movement.x < -0.01f)
            spriteRenderer.flipX = false; 


        bool isWalking = movement.sqrMagnitude > 0.01f;
        animator.SetBool("IsWalking", isWalking);

        if (Input.GetKeyDown(KeyCode.Tab) && !isHavingDeadBody)
        {
            DoSwap();
        }

        if (isHavingDeadBody)
            animator.SetBool("IsHavingDeadBody", true);
        else
            animator.SetBool("IsHavingDeadBody", false);

        //spriteRenderer.color = isKiller ? Color.black : Color.white;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement.normalized * speed;
    }

    void DoSwap()
    {
        animator.SetTrigger("Swap");
        isKiller = !isKiller;

        playerPickUp.enabled = !isKiller;
        playerKiller.enabled = isKiller;

        OnKillerStateChanged?.Invoke(isKiller);
    }

}
