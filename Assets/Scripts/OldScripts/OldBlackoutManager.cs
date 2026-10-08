using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OldBlackoutManager : MonoBehaviour
{
    public static OldBlackoutManager Instance;

    [Header("Light")]
    public Light2D globalLight;
    [Range(0f, 1f)] public float normalIntensity = 1f;
    [Range(0f, 1f)] public float blackoutIntensity = 0.05f;

    [Header("Timing")]
    public float blackoutDuration = 6f;
    public float cooldown = 10f;

    public static bool IsBlackout { get; private set; }

    public float CooldownProgress { get; private set; } // 0..1

    private Coroutine routine;
    private float nextUseTime;

    void Awake()
    {
        Instance = this;
        if (globalLight != null)
            globalLight.intensity = normalIntensity;
    }

    void Update()
    {
        if (Time.time < nextUseTime)
        {
            float total = cooldown;
            float left = nextUseTime - Time.time;
            CooldownProgress = Mathf.Clamp01(left / total); // 1 -> 0
        }
        else
        {
            CooldownProgress = 0f;
        }
    }

    public bool CanUse() => Time.time >= nextUseTime && routine == null;

    public void TriggerBlackout()
    {
        if (!CanUse()) return;
        routine = StartCoroutine(BlackoutRoutine());
    }

    IEnumerator BlackoutRoutine()
    {
        nextUseTime = Time.time + cooldown;

        IsBlackout = true;
        if (globalLight != null)
            globalLight.intensity = blackoutIntensity;

        yield return new WaitForSeconds(blackoutDuration);

        IsBlackout = false;
        if (globalLight != null)
            globalLight.intensity = normalIntensity;

        routine = null;
    }
}
