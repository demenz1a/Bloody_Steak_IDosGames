using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class OldNPCAI : MonoBehaviour
{
    [Header("Navigation")]
    public NavMeshAgent agent;

    public Transform orderPoint;
    public bool isWaiting;
    public string gameOverSceneName = "BadEnding";

    private Vector3[] tablesVectors = new Vector3[]
{
    new Vector3( -14.2f, 6.2f, 0f),
    new Vector3( -11.45f, 4.73f, 0f ),
    new Vector3( -8.64f, 5.99f, 0f ),

    new Vector3( -5.09f, 5.76f, 0f ),
    new Vector3( -2.54f, 4.42f, 0f ),
    new Vector3( 0.42f, 6f, 0f ),

    new Vector3( -14.03f, -0.42f, 0f ),
    new Vector3(  -11.51f, 0.4f, 0f ),
    new Vector3( -8.69f, 1.6f, 0f ),

    new Vector3( -4.76f, 1.22f, 0f ),
    new Vector3( -2.27f, 0.25f, 0f ),
    new Vector3( 0.37f, 1.57f, 0f ),
};

    private Vector3[] toiletsVectors = new Vector3[]
{
    new Vector3(5.28f, 6.48f, 0f),
    new Vector3(8.61f, 6.48f, 0f),
    new Vector3(5.28f, 0.55f, 0f),
    new Vector3(5.28f, 0.55f, 0f), // TODO: duplicate of the point above — likely a typo, set the correct coordinate for the 4th toilet spot
};

    public Vector3 exit;

    [Header("UI")]
    public GameObject orderTable;

    [Header("Settings")]
    public float eatingTime = 5f;
    public float toiletTime = 4f;
    public float panicSpeedMultiplier = 1.8f;

    public bool OrderCompleted;
    public bool OrderNotCompleted;
    public bool AbleToToilet;

    private State currentState;
    private float stateTimer;

    private Transform currentTarget;
    public OldOrderTrigger assignedOrderPoint;
    public GameObject DeathZone;

    public event Action<OldNPCAI> OnDestroyed;
    private Animator animator;
    public bool isPlayerKiller;

    private SpriteRenderer sr;

    public AudioSource audios;
    public AudioClip BeeClip;
    public AudioClip EatClip;
    public AudioClip bip;
    public AudioClip alertSound;

    [Header("Sprite Flip")]
    public bool faceRightByDefault = true;   // ���� ������ � ��������� ������� ������
    public float flipDeadZone = 0.02f;       // ����� �� �������� �� �����

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    // ===================== STATES =====================

    private enum State
    {
        Entry,
        Order,
        WalkingToTable,
        Eating,
        GoingToToilet,
        Toilet,
        GoingOut,
        Panic,
        Death
    }

    // ===================== UNITY =====================



    private void Start()
    {
        orderTable.SetActive(false);

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        SwitchState(State.Entry);
    }

    private void Update()
    {
        StateHandler();
        UpdateFlip();
    }

    void OnEnable()
    {
        OldPlayerMovement.OnKillerStateChanged += HandleKillerState;
    }

    void OnDisable()
    {
        OldPlayerMovement.OnKillerStateChanged -= HandleKillerState;
    }


    // ===================== FSM =====================

    private void StateHandler()
    {
        switch (currentState)
        {
            case State.Entry:
                if (ReachedDestination())
                {
                    audios.PlayOneShot(bip);
                    SwitchState(State.Order);
                    OnReachedOrderPoint(); // <- ������� �������� � ������
                }
                break;

            case State.Order:
                if (OrderCompleted)
                    SwitchState(State.WalkingToTable);
                if (OrderNotCompleted)
                    SwitchState(State.GoingOut);
                break;

            case State.WalkingToTable:
                if (ReachedDestination())
                    SwitchState(State.Eating);
                break;

            case State.Eating:
                stateTimer -= Time.deltaTime;
                audios.Play();
                if (stateTimer <= 0)
                {
                    audios.Stop();
                    bool canGoToToilet =
                        UnityEngine.Random.value < 0.5f;

                    SwitchState(canGoToToilet
                        ? State.GoingToToilet
                        : State.GoingOut);
                }
                break;

            case State.GoingToToilet:
                if (ReachedDestination())
                    SwitchState(State.Toilet);
                break;

            case State.Toilet:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                    SwitchState(State.GoingOut);
                break;

            case State.GoingOut:
                if (ReachedDestination())
                    Leave();
                break;

            case State.Panic:
                if (ReachedDestination())
                {
                    SceneManager.LoadScene(gameOverSceneName);
                }
                break;

            case State.Death:
                break;
        }
    }

    // ===================== STATE SWITCH =====================

    private void SwitchState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Entry:
                //currentTarget = orderVectors[GetOrderPoint(orderPoints)];
                //agent.SetDestination(orderVectors[GetOrderPoint(orderPoints)]);
                animator.SetBool("IsWalking", true);
                agent.SetDestination(orderPoint.position);
                break;

            case State.Order:
                agent.ResetPath();
                animator.SetBool("IsWalking", false);
                ShowOrderBubble(true);
                break;

            case State.WalkingToTable:
                //ShowOrderBubble(false);
                //currentTarget = GetRandom(tablesVectors);
                animator.SetBool("IsWalking", true);
                agent.SetDestination(GetRandom(tablesVectors));
                break;

            case State.Eating:
                stateTimer = eatingTime;
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsEating", true);
                PlayEatingAnimation();
                break;

            case State.GoingToToilet:
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsEating", false);
                //currentTarget = GetRandom(toilets);
                agent.SetDestination(GetRandom(toiletsVectors));
                break;

            case State.Toilet:
                animator.SetBool("IsWalking", false);
                stateTimer = toiletTime;
                break;

            case State.GoingOut:
                animator.SetBool("IsEating", false);
                animator.SetBool("IsWalking", true);
                agent.SetDestination(exit);
                break;

            case State.Panic:
                animator.SetBool("IsEating", false);
                animator.SetBool("IsWalking", true);
                ShowOrderBubble(false);
                agent.speed *= panicSpeedMultiplier;
                agent.SetDestination(exit);
                break;

            case State.Death:
                animator.SetBool("IsWalking", false);
                agent.ResetPath();
                break;
        }
    }

    // ===================== HELPERS =====================

    private bool ReachedDestination()
    {
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance;
    }

    private Vector3 GetRandom(Vector3[] points)
    {
        return points[UnityEngine.Random.Range(0, points.Length)];
    }

    public void ShowOrderBubble(bool value)
    {
        if (orderTable != null)
            orderTable.SetActive(value);
    }

    private void Leave()
    {
        if (assignedOrderPoint != null)
            assignedOrderPoint.ForceRelease(this);

        OnDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    // ===================== EXTERNAL =====================

    public void Panic()
    {
        if (currentState == State.Panic || currentState == State.Death)
            return;

        audios.PlayOneShot(alertSound);
        StartCoroutine(DoPanic());
    }

    IEnumerator DoPanic()
    {
        animator.SetTrigger("IsScared");
        yield return new WaitForSeconds(1f);
        SwitchState(State.Panic);
        Debug.Log("NPC PANIC!");
    }

    public void DoKill()
    {
        StartCoroutine(Kill());
    }

    IEnumerator Kill()
    {
        currentState = State.Death;
        agent.ResetPath();

        if (assignedOrderPoint != null)
            assignedOrderPoint.ForceRelease(this);

        DeathZone.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    private void PlayEatingAnimation()
    {
        // animator.SetTrigger("Eat");
    }

    void HandleKillerState(bool isKiller)
    {
        if (OrderCompleted)
            return;

        if (OrderNotCompleted)
            return;
        ShowOrderBubble(!isKiller);
    }

    public void MarkOrderCompleted()
    {
        OrderCompleted = true;
        OrderNotCompleted = false;
        ShowOrderBubble(false);
    }

    public void MarkOrderFailed()
    {
        OrderNotCompleted = true;
        OrderCompleted = false;
        ShowOrderBubble(false);
    }

    public void GoOutNow()
    {
        if (currentState == State.Death) return;

        ShowOrderBubble(false);
        SwitchState(State.GoingOut);
    }

    private void UpdateFlip()
    {
        if (sr == null) return;
        if (currentState == State.Death) return; // �������� �� �������

        // ���� ����� ����� ����� � �� ������ �����������
        Vector2 v = agent.velocity;
        if (Mathf.Abs(v.x) < flipDeadZone) return;

        // ���� �������� ������ � flipX = false (���� faceRightByDefault = true)
        bool movingRight = v.x > 0f;

        if (faceRightByDefault)
            sr.flipX = !movingRight;   // ����� = flip
        else
            sr.flipX = movingRight;    // ���� ������ ���������� ������� �����
    }

    protected virtual void OnReachedOrderPoint()
    {
        // ������� NPC ������ �� ������
    }
}