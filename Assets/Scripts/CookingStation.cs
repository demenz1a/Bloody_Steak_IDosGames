using UnityEngine;

/// <summary>
/// Общая кухонная станция. Гриль, шампуры и мясорубка — это ОДИН И ТОТ ЖЕ компонент
/// с разными данными в инспекторе (stages), а не разные скрипты.
///
/// Продукт не создаётся и не уничтожается станцией — тот же физический Product,
/// который игрок принёс в руках, перемещается в holdPoint станции и меняет свой
/// Type по ходу таймера. Когда игрок забирает готовое — тот же объект уходит
/// обратно в руки игрока.
///
/// Путь состояния: Empty -> Cooking -> (одна или несколько servable-стадий) -> Burnt.
/// </summary>
public class CookingStation : MonoBehaviour, IInteractable
{
    [Header("Recipe")]
    [Tooltip("По возрастанию timeToReach")]
    [SerializeField] private CookingStage[] stages;
    [Tooltip("Через сколько секунд ПОСЛЕ последней стадии продукт сгорает, если его не забрать")]
    [SerializeField] private float burnDelay = 5f;

    [Header("Placement")]
    [SerializeField] private HoldPoint holdPoint;

    [Header("UI")]
    [SerializeField] private CookingTimerUI timerUI;

    [Header("Effects")]
    [SerializeField] private ParticleSystem cookingParticles;

    [Header("Shake (если нет ParticleSystem)")]
    [SerializeField] private float shakeAmplitude = 0.03f;
    [SerializeField] private float shakeSpeed = 25f;

    private Vector3 _originalPosition;

    private StationPhase _phase = StationPhase.Empty;
    private float _elapsed;
    private int _currentStageIndex; // -1 = стадия ещё не достигнута (мясо только положили)

    public bool IsEmpty => _phase == StationPhase.Empty;

    private void Awake()
    {
        _originalPosition = transform.localPosition;
    }

    public bool CanInteract(PlayerInteractor player)
    {
        switch (_phase)
        {
            case StationPhase.Empty:
                return player.Inventory.HasItem
                    && player.Inventory.CurrentProduct != null
                    && player.Inventory.CurrentProduct.Type == ProductType.RawMeat;

            case StationPhase.ReadyToServe:
            case StationPhase.Burnt:
                return !player.Inventory.HasItem;

            default: // Cooking — руками трогать нечего
                return false;
        }
    }

    public void Interact(PlayerInteractor player)
    {
        if (_phase == StationPhase.Empty)
        {
            var item = player.Inventory.TakeFromHands();
            holdPoint.Place(item);
            StartCooking();
            return;
        }

        if (_phase == StationPhase.ReadyToServe || _phase == StationPhase.Burnt)
        {
            var item = holdPoint.TakeItem();
            if (player.Inventory.TryPickUp(item))
            {
                ResetStation();
            }
            else
            {
                holdPoint.Place(item); // страховка: руки внезапно оказались заняты — кладём назад
            }
        }
    }

    private void StartCooking()
    {
        _phase = StationPhase.Cooking;
        _elapsed = 0f;
        _currentStageIndex = -1;

        timerUI?.SetVisible(true);

        if (cookingParticles != null)
        {
            cookingParticles.Play();
        }
    }

    private void ResetStation()
    {
        _phase = StationPhase.Empty;
        _elapsed = 0f;
        _currentStageIndex = -1;

        timerUI?.SetVisible(false);

        if (cookingParticles != null)
        {
            cookingParticles.Stop();
        }

        transform.localPosition = _originalPosition;
    }

    private void Update()
    {
        UpdateVisualEffect();

        if (_phase != StationPhase.Cooking && _phase != StationPhase.ReadyToServe)
            return;

        if (stages == null || stages.Length == 0)
            return;

        _elapsed += Time.deltaTime;

        int newStageIndex = _currentStageIndex;
        for (int i = _currentStageIndex + 1; i < stages.Length; i++)
        {
            if (_elapsed >= stages[i].timeToReach) newStageIndex = i;
            else break;
        }

        if (newStageIndex != _currentStageIndex)
        {
            _currentStageIndex = newStageIndex;
            var product = holdPoint.CurrentItem as Product;
            product?.SetType(stages[_currentStageIndex].resultType);
            _phase = stages[_currentStageIndex].isServable
                ? StationPhase.ReadyToServe
                : StationPhase.Cooking;
        }

        float lastStageTime = stages[stages.Length - 1].timeToReach;

        if (_elapsed >= lastStageTime + burnDelay)
        {
            _phase = StationPhase.Burnt;
            var product = holdPoint.CurrentItem as Product;
            product?.SetType(ProductType.Burned);
            timerUI?.SetVisible(false);
            if (cookingParticles != null)
            {
                cookingParticles.startColor = Color.black;
            }

            transform.localPosition = _originalPosition;
            return;
        }

        UpdateTimerUI(lastStageTime);
    }

    private void UpdateTimerUI(float lastStageTime)
    {
        if (timerUI == null) return;

        float segmentStart;
        float segmentEnd;
        bool isBurnWarning;

        if (_currentStageIndex < stages.Length - 1)
        {
            segmentStart = _currentStageIndex >= 0 ? stages[_currentStageIndex].timeToReach : 0f;
            segmentEnd = stages[_currentStageIndex + 1].timeToReach;
            isBurnWarning = false;
        }
        else
        {
            segmentStart = lastStageTime;
            segmentEnd = lastStageTime + burnDelay;
            isBurnWarning = true;
        }

        float progress = Mathf.InverseLerp(segmentStart, segmentEnd, _elapsed);
        timerUI.SetProgress(progress, isBurnWarning);
    }

    private void UpdateVisualEffect()
    {
        if (cookingParticles != null)
            return;

        if (_phase == StationPhase.Cooking || _phase == StationPhase.ReadyToServe)
        {
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmplitude;
            transform.localPosition = _originalPosition + new Vector3(offsetX, 0f, 0f);
        }
        else
        {
            transform.localPosition = _originalPosition;
        }
    }
}
