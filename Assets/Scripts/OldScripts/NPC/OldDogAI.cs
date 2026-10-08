using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class OldDogAI : MonoBehaviour
{
    [Header("Navigation")]
    public NavMeshAgent agent;
    public Vector3 entryPoint = new Vector3(-15.38f, 7.95f, 0);
    public Vector3 standPoint = new Vector3(-11.08f, 7.95f, 5);

    [Header("Camera")]
    public OldCinemachineFocus cameraFocus;

    [Header("Timings")]
    public float introLookTime = 1.2f;
    public float standDuration = 10f;
    public float caughtLookTime = 1.0f;

    [Header("Game Over")]
    public string gameOverSceneName = "GameOver";

    private Animator animator;
    private bool finished;
    private Coroutine routine;

    public AudioSource audioSource;
    public AudioClip audioShot;
    public AudioClip audioLeave;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    private void OnEnable()
    {
        finished = false;

        OldKillerLockdown.OnMurderDuringLockdown += OnCaught;

        // каждый раз при активации запускаем сценарий
        routine = StartCoroutine(Routine());
    }

    private void OnDisable()
    {
        OldKillerLockdown.OnMurderDuringLockdown -= OnCaught;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // === ОСНОВНАЯ ЛОГИКА ===

    private IEnumerator Routine()
    {
        // 1) вход
        SetWalking(true);
        agent.Warp(entryPoint);
        agent.SetDestination(entryPoint);
        yield return WaitUntilReached();

        // 2) фокус камеры
        if (cameraFocus != null)
            cameraFocus.FocusInspector(introLookTime);

        yield return new WaitForSeconds(introLookTime);

        // 3) подойти и встать
        SetWalking(true);
        agent.SetDestination(standPoint);
        yield return WaitUntilReached();

        // 4) запрет убийств
        SetWalking(false);
        OldKillerLockdown.Begin();

        float t = standDuration;
        while (t > 0f && !finished)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (finished) yield break;

        // 5) уход
        OldKillerLockdown.End();
        SetWalking(true);
        audioSource.PlayOneShot(audioLeave);
        agent.SetDestination(entryPoint);
        yield return WaitUntilReached();

        Hide(); // 🔥 вместо Destroy
    }

    private void OnCaught()
    {
        if (finished) return;
        finished = true;
        StartCoroutine(CaughtRoutine());
    }

    private IEnumerator CaughtRoutine()
    {
        if (cameraFocus != null)
            cameraFocus.FocusInspector(caughtLookTime);

        animator.SetTrigger("Kill");
        OldKillerLockdown.Begin();

        yield return new WaitForSeconds(caughtLookTime);

        OldKillerLockdown.End();
        // NOTE: scene load is intentionally left to OpenScene(), which should be
        // wired as an Animation Event on the "Kill" clip. If that event isn't set up
        // in the Animator, call OpenScene() here instead:
        // OpenScene();
    }

    private IEnumerator WaitUntilReached()
    {
        while (agent.pathPending)
            yield return null;

        while (agent.remainingDistance > agent.stoppingDistance)
            yield return null;
    }

    private void SetWalking(bool walking)
    {
        animator.SetBool("IsWalking", walking);
    }

    public void PlayShot()
    {
        audioSource.PlayOneShot(audioShot);
    }

    public void OpenScene()
    {
        SceneManager.LoadScene(gameOverSceneName);
    }
}