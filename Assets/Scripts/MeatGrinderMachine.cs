using UnityEngine;

/// <summary>
/// Мясоперерабатывающая машина в кладовке. Принимает труп (состояние PlayerCorpseState,
/// не предмет из PlayerInventory — см. п. 13.4), перерабатывает его за processingDuration
/// секунд, после чего пополняет MeatTable. Пока идёт переработка, машину нельзя использовать повторно.
/// </summary>
public class MeatGrinderMachine : MonoBehaviour, IInteractable
{
    [SerializeField] private MeatTable meatTable;
    [SerializeField] private int meatYieldPerCorpse = 4;
    [SerializeField] private float processingDuration = 5f;
    [SerializeField] private CookingTimerUI timerUI; // переиспользуем тот же радиальный индикатор
    [SerializeField] private GameObject particleSystem;

    private bool _isProcessing;
    private float _elapsed;

    public bool CanInteract(PlayerInteractor player)
    {
        if (_isProcessing) return false;
        return player.Corpse.IsCarrying;
    }

    public void Interact(PlayerInteractor player)
    {
        if (!player.Corpse.IsCarrying) return;

        player.Corpse.ConsumeCorpse();

        _isProcessing = true;
        _elapsed = 0f;
        timerUI?.SetVisible(true);
        particleSystem.SetActive(true);
    }

    private void Update()
    {
        if (!_isProcessing) return;

        _elapsed += Time.deltaTime;
        timerUI?.SetProgress(_elapsed / processingDuration, isBurnWarning: false);

        if (_elapsed >= processingDuration)
        {
            meatTable.AddMeat(meatYieldPerCorpse);
            _isProcessing = false;
            timerUI?.SetVisible(false);
            particleSystem.SetActive(false);
        }
    }
}
