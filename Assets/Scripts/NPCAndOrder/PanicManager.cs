using System.Collections.Generic;
using UnityEngine;

/// <summary>Насколько серьёзна причина, по которой конкретный NPC запаниковал.</summary>
public enum PanicSeverity
{
    Normal = 1,        // долгая экспозиция трупа рядом (см. NpcPanicZone)
    WitnessedKill = 2  // увидел непосредственно убийство (см. ScanForWitnesses)
}

/// <summary>
/// Единственный источник правды по общей шкале подозрительности. Значение считается
/// заново каждый кадр из текущего состояния — ничего не хранится как таймер самой шкалы,
/// поэтому она естественно падает к 0, когда угроза миновала:
///
/// - никто не паникует и никто не "на грани" -> 0
/// - хотя бы один NPC копит экспозицию трупа, но ещё не запаниковал -> плавно 0..1 (прогресс)
/// - хотя бы один NPC уже паникует (неважно, из-за чего и сколько их) -> 1 (шкала полная)
///
/// Мгновенный скачок к 1 при убийстве визуально сглаживается не здесь, а в PanicBarUI —
/// это чисто отображение, а не часть игровой логики.
/// </summary>
public class PanicManager : MonoBehaviour
{
    public static PanicManager Instance { get; private set; }

    [Tooltip("Слой, на котором находятся коллайдеры NpcPanicZone — используется сканом при убийстве.")]
    [SerializeField] private LayerMask panicZoneMask;

    private readonly List<NpcCustomer> _panickingNpcs = new List<NpcCustomer>();
    private readonly Dictionary<NpcCustomer, PanicSeverity> _severityByNpc = new Dictionary<NpcCustomer, PanicSeverity>();
    private readonly Dictionary<NpcCustomer, float> _exposureProgress = new Dictionary<NpcCustomer, float>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Вызывается NpcPanicZone каждый кадр — прогресс экспозиции трупа у конкретного NPC (0..1).</summary>
    public void ReportExposure(NpcCustomer npc, float progress01)
    {
        _exposureProgress[npc] = progress01;
    }

    /// <summary>Вызывается NpcCustomer.EnterPanic — регистрирует нового паникующего.</summary>
    public void RegisterPanic(NpcCustomer npc, PanicSeverity severity)
    {
        if (!_panickingNpcs.Contains(npc)) _panickingNpcs.Add(npc);
        _severityByNpc[npc] = severity; // не влияет на саму шкалу, но может пригодиться (напр. для полиции)
        _exposureProgress.Remove(npc); // больше не "на грани" — уже паникует по-настоящему
    }

    /// <summary>Вызывается, когда паникующий NPC покидает сцену (см. NpcCustomer, состояние Panicking).</summary>
    public void UnregisterPanic(NpcCustomer npc)
    {
        _panickingNpcs.Remove(npc);
        _severityByNpc.Remove(npc);
    }

    /// <summary>Текущее "целевое" значение шкалы подозрительности (0..1). UI сглаживает переход сам.</summary>
    public float GetCurrentBarValue()
    {
        if (_panickingNpcs.Count > 0) return 1f; // хоть один паникует — шкала полная, независимо от количества и причины

        float maxProgress = 0f;
        foreach (var progress in _exposureProgress.Values)
        {
            if (progress > maxProgress) maxProgress = progress;
        }
        return maxProgress;
    }

    /// <summary>
    /// Скан радиуса вокруг места убийства: все NPC с зоной паники в радиусе мгновенно
    /// впадают в панику как свидетели убийства — без накопления, в отличие от экспозиции трупа.
    /// </summary>
    public void ScanForWitnesses(Vector2 killPosition, float radius, NpcCustomer excludeVictim)
    {
        var hits = Physics2D.OverlapCircleAll(killPosition, radius, panicZoneMask);

        foreach (var hit in hits)
        {
            var zone = hit.GetComponent<NpcPanicZone>();
            if (zone == null) continue;

            var witness = zone.Owner;
            if (witness == null || witness == excludeVictim) continue;
            if (witness.IsPanicking || witness.IsDead) continue;

            witness.EnterPanic(PanicSeverity.WitnessedKill);
        }
    }
}
