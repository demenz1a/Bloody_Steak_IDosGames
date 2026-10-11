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
    [Tooltip("Тело (родитель рук), которое скин сдвигает в форме повара (PlayerSkin.ChefBodyOffset). Пусто — ребёнок с именем Chest.")]
    [SerializeField] private Transform chest;
    [Tooltip("Голова: со скином она рисуется поверх всех частей тела. Пусто — ребёнок с именем Head.")]
    [SerializeField] private SpriteRenderer head;
    [Tooltip("Только для проверки в редакторе: надеть этот скин вместо выбранного в оболочке.")]
    [SerializeField] private string editorPreviewSkinId;

    private Sprite[] _base;
    private Sprite[] _applied;
    private PlayerSkin _skin;

    // Сдвиг тела и порядок головы ставятся поверх аниматора тем же способом, что и спрайты:
    // запоминаем, что записали сами, чтобы отличить это от нового значения аниматора.
    private Vector3 _chestWritten;
    private Vector3 _chestAppliedOffset;
    private int _headWrittenOrder;
    private int _headBaseOrder;
    private bool _headOverridden;

    private void Awake()
    {
        if (parts == null || parts.Length == 0) parts = GetComponentsInChildren<SpriteRenderer>(true);
        _base = new Sprite[parts.Length];
        _applied = new Sprite[parts.Length];

        if (chest == null) chest = FindChild(transform, "Chest");
        if (head == null)
        {
            var headTransform = FindChild(transform, "Head");
            if (headTransform != null) head = headTransform.GetComponent<SpriteRenderer>();
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
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

        ApplyChestOffset(_skin != null && chefForm ? (Vector3)_skin.ChefBodyOffset : Vector3.zero);
        ApplyHeadOnTop(_skin != null);
    }

    /// <summary>
    /// Сдвигает тело вместе с руками поверх позиции, выставленной аниматором. Если аниматор в этом
    /// кадре позицию не писал (там всё ещё наша прошлая запись) — сначала снимаем прошлый сдвиг,
    /// иначе он копился бы каждый кадр.
    /// </summary>
    private void ApplyChestOffset(Vector3 offset)
    {
        if (chest == null) return;

        var current = chest.localPosition;
        var basePosition = current == _chestWritten ? current - _chestAppliedOffset : current;
        if (offset == Vector3.zero && _chestAppliedOffset == Vector3.zero) return;

        chest.localPosition = basePosition + offset;
        _chestWritten = chest.localPosition;
        _chestAppliedOffset = offset;
    }

    /// <summary>Со скином голова рисуется поверх всех частей тела; без скина — возвращается порядок аниматора/префаба.</summary>
    private void ApplyHeadOnTop(bool onTop)
    {
        if (head == null) return;

        if (!_headOverridden || head.sortingOrder != _headWrittenOrder) _headBaseOrder = head.sortingOrder;

        if (!onTop)
        {
            if (_headOverridden) head.sortingOrder = _headBaseOrder;
            _headOverridden = false;
            return;
        }

        int top = int.MinValue;
        foreach (var r in parts)
        {
            if (r != null && r != head && r.sortingLayerID == head.sortingLayerID && r.sortingOrder > top) top = r.sortingOrder;
        }

        int order = top == int.MinValue ? _headBaseOrder : Mathf.Max(_headBaseOrder, top + 1);
        head.sortingOrder = order;
        _headWrittenOrder = order;
        _headOverridden = true;
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
