using UnityEngine;

/// <summary>
/// Зона паники конкретного NPC (дочерний объект с Collider2D Trigger, слой должен совпадать
/// с panicZoneMask в PanicManager). Отвечает ТОЛЬКО за один способ паники — экспозицию трупа:
/// пока игрок с трупом в руках стоит внутри, копится таймер; по достижении requiredExposureTime
/// NPC переходит в панику. Второй способ (мгновенная паника от скана при убийстве) реализован
/// в PanicManager.ScanForWitnesses и этого компонента не касается.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcPanicZone : MonoBehaviour
{
    [SerializeField] private NpcCustomer owner;
    [SerializeField] private float requiredExposureTime = 1f;

    private PlayerInteractor _playerInZone;
    private float _exposureTimer;

    public NpcCustomer Owner => owner;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerInteractor>();
        if (player != null) _playerInZone = player;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerInteractor>();
        if (player != null && player == _playerInZone)
        {
            _playerInZone = null;
            _exposureTimer = 0f;
            PanicManager.Instance.ReportExposure(owner, 0f);
        }
    }

    private void Update()
    {
        if (owner.IsPanicking || owner.IsDead)
        {
            _exposureTimer = 0f;
            return; // уже среагировал (или мёртв) — копить больше нечего
        }

        bool exposedNow = _playerInZone != null && _playerInZone.Corpse.IsCarrying;

        if (exposedNow)
        {
            _exposureTimer += Time.deltaTime;
            PanicManager.Instance.ReportExposure(owner, Mathf.Clamp01(_exposureTimer / requiredExposureTime));

            if (_exposureTimer >= requiredExposureTime)
            {
                owner.EnterPanic(PanicSeverity.Normal);
            }
        }
        else
        {
            _exposureTimer = 0f;
            PanicManager.Instance.ReportExposure(owner, 0f);
        }
    }
}
