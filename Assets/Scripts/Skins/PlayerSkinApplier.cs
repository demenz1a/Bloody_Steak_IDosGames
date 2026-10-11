using UnityEngine;

/// <summary>
/// Надевает на игрока выбранный скин (PlayerSkinSelection), не трогая анимации.
///
/// Аниматор каждый кадр ставит частям тела базовые спрайты (в т.ч. меняет голову и тело
/// при смене формы). В LateUpdate, уже после него, компонент подменяет базовый спрайт
/// каждой части на спрайт скина. Чтобы отличить "аниматор поставил новый базовый спрайт"
/// от "это наша же подмена с прошлого кадра", для каждого рендерера запоминается
/// и базовый спрайт, и поставленная замена.
///
/// Форма повара определяется не по PlayerFormController, а по тому, что сейчас показывает
/// тело (chefMarkers — базовые спрайты головы/тела повара): так руки повара меняются ровно
/// в кадр анимации смены формы, а не раньше неё.
/// </summary>
public class PlayerSkinApplier : MonoBehaviour
{
    [Tooltip("Части тела. Пусто — берутся все SpriteRenderer'ы в детях.")]
    [SerializeField] private SpriteRenderer[] parts;
    [Tooltip("Базовые спрайты, по которым видно, что игрок сейчас в форме повара (голова и тело повара).")]
    [SerializeField] private Sprite[] chefMarkers;
    [Tooltip("Только для проверки в редакторе: надеть этот скин вместо выбранного в оболочке.")]
    [SerializeField] private string editorPreviewSkinId;

    private Sprite[] _base;
    private Sprite[] _applied;
    private PlayerSkin _skin;

    private void Awake()
    {
        if (parts == null || parts.Length == 0) parts = GetComponentsInChildren<SpriteRenderer>(true);
        _base = new Sprite[parts.Length];
        _applied = new Sprite[parts.Length];
    }

    private void OnEnable()
    {
        PlayerSkinSelection.Changed += HandleSkinChanged;
        HandleSkinChanged(PlayerSkinSelection.Current);
    }

    private void OnDisable() => PlayerSkinSelection.Changed -= HandleSkinChanged;

    private void HandleSkinChanged(string skinId)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(editorPreviewSkinId)) skinId = editorPreviewSkinId;
#endif
        var catalog = PlayerSkinCatalog.Instance;
        _skin = catalog != null ? catalog.Find(skinId) : null;
    }

    private void LateUpdate()
    {
        // 1. Базовые спрайты этого кадра: если на рендерере не наша прошлая подмена — его поставил аниматор.
        bool chefForm = false;
        for (int i = 0; i < parts.Length; i++)
        {
            var r = parts[i];
            if (r == null) continue;

            if (r.sprite != _applied[i] || _applied[i] == null) _base[i] = r.sprite;
            if (IsChefMarker(_base[i])) chefForm = true;
        }

        // 2. Подмена по скину; без скина — возврат базовых спрайтов.
        for (int i = 0; i < parts.Length; i++)
        {
            var r = parts[i];
            if (r == null) continue;

            var target = _skin != null ? _skin.Resolve(_base[i], chefForm) : null;
            var sprite = target != null ? target : _base[i];
            if (r.sprite != sprite) r.sprite = sprite;
            _applied[i] = target != null ? target : null;
        }
    }

    private bool IsChefMarker(Sprite sprite)
    {
        if (sprite == null || chefMarkers == null) return false;

        foreach (var marker in chefMarkers)
        {
            if (marker == sprite) return true;
        }
        return false;
    }
}
