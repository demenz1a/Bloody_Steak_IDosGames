using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Надевает на игрока выбранный скин (PlayerSkinSelection).
///
/// Аниматор каждый кадр ставит частям тела базовые спрайты (в т.ч. меняет голову и тело
/// при смене формы). В LateUpdate, уже после него, компонент подменяет базовый спрайт
/// каждой части на спрайт скина. Чтобы отличить "аниматор поставил новый базовый спрайт"
/// от "это наша же подмена с прошлого кадра", для каждого рендерера запоминается
/// и базовый спрайт, и поставленная замена.
///
/// Скины нарисованы под позу волка, поэтому со скином форма повара играет стойку и ходьбу
/// волка: аниматор получает AnimatorOverrideController, где клипы повара (chefIdleClips,
/// chefWalkClips) заменены на клипы волка. Спрайты волка в форме повара заменяются на
/// поварские спрайты скина, нарезанные по прямоугольникам волка (chefSwaps скина).
///
/// Форма повара — это состояния аниматора chefStates или кадры, где тело показывает базовые
/// спрайты повара (chefMarkers, анимации смены формы). В таких кадрах части стоят в позе
/// повара, и для них остаются сдвиги PlayerSkin.ChefChestOffset/руки.
/// </summary>
public class PlayerSkinApplier : MonoBehaviour
{
    [Tooltip("Части тела. Пусто — берутся все SpriteRenderer'ы в детях.")]
    [SerializeField] private SpriteRenderer[] parts;
    [Tooltip("Базовые спрайты, по которым видно, что игрок сейчас в форме повара (голова и тело повара).")]
    [SerializeField] private Sprite[] chefMarkers;
    [Tooltip("Тело (родитель рук), которое скин сдвигает в форме повара (PlayerSkin.ChefChestOffset). Пусто — ребёнок с именем Chest.")]
    [SerializeField] private Transform chest;
    [Tooltip("Левая рука (PlayerSkin.ChefLeftArmOffset). Пусто — ребёнок с именем LeftHand.")]
    [SerializeField] private Transform leftArm;
    [Tooltip("Правая рука (PlayerSkin.ChefRightArmOffset). Пусто — ребёнок с именем RightHand.")]
    [SerializeField] private Transform rightArm;
    [Tooltip("Голова: со скином она рисуется поверх всех частей тела (кроме transitionRenderers). Пусто — ребёнок с именем Head.")]
    [SerializeField] private SpriteRenderer head;
    [Tooltip("Эффекты смены формы: со скином рисуются ещё выше головы. Пусто — дети с именами Transition и Transition2.")]
    [SerializeField] private SpriteRenderer[] transitionRenderers;
    [Tooltip("Только для проверки в редакторе: надеть этот скин вместо выбранного в оболочке.")]
    [SerializeField] private string editorPreviewSkinId;

    [Header("Скин в форме повара: анимации волка")]
    [Tooltip("Аниматор модельки. Пусто — первый Animator в детях.")]
    [SerializeField] private Animator animator;
    [Tooltip("Состояния аниматора, в которых игрок в форме повара.")]
    [SerializeField] private string[] chefStates = { "SheepCookAFK", "SheepCookWalk" };
    [Tooltip("Клипы стойки повара: со скином заменяются пустым клипом — как стойка волка (WolfKillerAFK без клипа).")]
    [SerializeField] private string[] chefIdleClips = { "SheepCookAFK" };
    [Tooltip("Клипы ходьбы повара: со скином заменяются на wolfWalkClip.")]
    [SerializeField] private string[] chefWalkClips = { "SheepCookWalk", "SheepCookWalking" };
    [SerializeField] private string wolfWalkClip = "WolfKillerWalk";

    private Sprite[] _base;
    private Sprite[] _applied;
    private PlayerSkin _skin;
    private RuntimeAnimatorController _baseController;
    private AnimatorOverrideController _skinController;
    private int[] _chefStateHashes;

    // Сдвиги частей и порядок головы ставятся поверх аниматора тем же способом, что и спрайты:
    // запоминаем, что записали сами, чтобы отличить это от нового значения аниматора.
    private readonly OffsetSlot _chestSlot = new OffsetSlot();
    private readonly OffsetSlot _leftArmSlot = new OffsetSlot();
    private readonly OffsetSlot _rightArmSlot = new OffsetSlot();
    private readonly OffsetSlot _headSlot = new OffsetSlot();
    private SortingSlot _headSorting;
    private SortingSlot[] _transitionSorting;

    private void Awake()
    {
        if (parts == null || parts.Length == 0) parts = GetComponentsInChildren<SpriteRenderer>(true);
        _base = new Sprite[parts.Length];
        _applied = new Sprite[parts.Length];

        if (chest == null) chest = FindChild(transform, "Chest");
        if (leftArm == null) leftArm = FindChild(transform, "LeftHand");
        if (rightArm == null) rightArm = FindChild(transform, "RightHand");
        _chestSlot.target = chest;
        _leftArmSlot.target = leftArm;
        _rightArmSlot.target = rightArm;
        if (head == null)
        {
            var headTransform = FindChild(transform, "Head");
            if (headTransform != null) head = headTransform.GetComponent<SpriteRenderer>();
        }
        if (head != null) _headSlot.target = head.transform;
        _headSorting = new SortingSlot { renderer = head };

        if (transitionRenderers == null || transitionRenderers.Length == 0)
        {
            var found = new List<SpriteRenderer>();
            foreach (var name in new[] { "Transition", "Transition2" })
            {
                var t = FindChild(transform, name);
                var r = t != null ? t.GetComponent<SpriteRenderer>() : null;
                if (r != null) found.Add(r);
            }
            transitionRenderers = found.ToArray();
        }
        _transitionSorting = new SortingSlot[transitionRenderers.Length];
        for (int i = 0; i < transitionRenderers.Length; i++) _transitionSorting[i] = new SortingSlot { renderer = transitionRenderers[i] };

        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator != null) _baseController = animator.runtimeAnimatorController;
        _chefStateHashes = new int[chefStates.Length];
        for (int i = 0; i < chefStates.Length; i++) _chefStateHashes[i] = Animator.StringToHash(chefStates[i]);
    }

    /// <summary>Контроллер со скином: те же состояния и переходы, но стойка и ходьба повара — от волка.</summary>
    private AnimatorOverrideController BuildSkinController()
    {
        AnimationClip wolfWalk = null;
        foreach (var clip in _baseController.animationClips)
        {
            if (clip.name == wolfWalkClip) wolfWalk = clip;
        }

        var controller = new AnimatorOverrideController(_baseController) { name = "SkinChefAsWolf" };
        var emptyIdle = new AnimationClip { name = "SkinChefIdle" };
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            string clipName = overrides[i].Key.name;
            if (System.Array.IndexOf(chefIdleClips, clipName) >= 0)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, emptyIdle);
            else if (wolfWalk != null && System.Array.IndexOf(chefWalkClips, clipName) >= 0)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, wolfWalk);
        }
        controller.ApplyOverrides(overrides);
        return controller;
    }

    /// <summary>
    /// Меняет контроллер аниматора, сохраняя текущее состояние и параметры: замена контроллера
    /// сбрасывает аниматор в начальное состояние, а скин могут сменить посреди смены.
    /// </summary>
    private void SetAnimatorController(RuntimeAnimatorController controller)
    {
        if (animator == null || controller == null || animator.runtimeAnimatorController == controller) return;

        bool wasActive = animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
        int stateHash = 0;
        float normalizedTime = 0f;
        var parameters = new List<(AnimatorControllerParameterType type, int hash, float f, int n, bool b)>();
        if (wasActive)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            stateHash = info.fullPathHash;
            normalizedTime = info.normalizedTime;
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger) continue;
                parameters.Add((p.type, p.nameHash, animator.GetFloat(p.nameHash), animator.GetInteger(p.nameHash), animator.GetBool(p.nameHash)));
            }
        }

        animator.runtimeAnimatorController = controller;
        if (!wasActive) return;

        foreach (var p in parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float: animator.SetFloat(p.hash, p.f); break;
                case AnimatorControllerParameterType.Int: animator.SetInteger(p.hash, p.n); break;
                case AnimatorControllerParameterType.Bool: animator.SetBool(p.hash, p.b); break;
            }
        }
        animator.Play(stateHash, 0, normalizedTime);
    }

    private bool IsInChefState()
    {
        if (animator == null || !animator.isActiveAndEnabled) return false;

        int hash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        foreach (int chefHash in _chefStateHashes)
        {
            if (hash == chefHash) return true;
        }
        return false;
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

        if (_baseController == null) return;
        if (_skin != null && _skinController == null) _skinController = BuildSkinController();
        SetAnimatorController(_skin != null ? _skinController : _baseController);
    }

    private void LateUpdate()
    {
        // 1. Базовые спрайты этого кадра: если на рендерере не наша прошлая подмена — его поставил аниматор.
        bool chefPose = false; // тело показывает спрайты повара — части стоят в позе повара
        for (int i = 0; i < parts.Length; i++)
        {
            var r = parts[i];
            if (r == null) continue;

            if (r.sprite != _applied[i] || _applied[i] == null) _base[i] = r.sprite;
            if (IsChefMarker(_base[i])) chefPose = true;
        }
        bool chefForm = chefPose || IsInChefState();

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

        // Сдвиги нужны только кадрам в позе повара (анимации смены формы); стойка и ходьба со скином — уже волчьи.
        bool shift = _skin != null && chefPose;
        _chestSlot.Apply(shift ? (Vector3)_skin.ChefChestOffset : Vector3.zero);
        _leftArmSlot.Apply(shift ? (Vector3)_skin.ChefLeftArmOffset : Vector3.zero);
        _rightArmSlot.Apply(shift ? (Vector3)_skin.ChefRightArmOffset : Vector3.zero);
        _headSlot.Apply(_skin != null ? (Vector3)_skin.HeadOffset : Vector3.zero);
        ApplySorting(_skin != null);
    }

    /// <summary>
    /// Сдвиг одной части поверх позиции, выставленной аниматором. Если аниматор в этом кадре позицию
    /// не писал (там всё ещё наша прошлая запись) — сначала снимается прошлый сдвиг, иначе он копился бы каждый кадр.
    /// </summary>
    private class OffsetSlot
    {
        public Transform target;
        private Vector3 _written;
        private Vector3 _applied;

        public void Apply(Vector3 offset)
        {
            if (target == null) return;
            if (offset == Vector3.zero && _applied == Vector3.zero) return;

            var current = target.localPosition;
            var basePosition = current == _written ? current - _applied : current;

            target.localPosition = basePosition + offset;
            _written = target.localPosition;
            _applied = offset;
        }
    }

    /// <summary>
    /// Порядок отрисовки поверх аниматора: запоминаем записанное, чтобы отличить от нового значения
    /// аниматора, и возвращаем исходный порядок, когда переопределение снято.
    /// </summary>
    private class SortingSlot
    {
        public SpriteRenderer renderer;
        public int BaseOrder { get; private set; }
        private int _written;
        private bool _overridden;

        public void Refresh()
        {
            if (renderer == null) return;
            if (!_overridden || renderer.sortingOrder != _written) BaseOrder = renderer.sortingOrder;
        }

        public void Set(int order)
        {
            if (renderer == null) return;
            renderer.sortingOrder = order;
            _written = order;
            _overridden = true;
        }

        public void Restore()
        {
            if (renderer == null || !_overridden) return;
            renderer.sortingOrder = BaseOrder;
            _overridden = false;
        }
    }

    /// <summary>
    /// Со скином голова рисуется поверх всех частей тела, а эффекты смены формы (Transition) — ещё выше
    /// головы, в своём исходном порядке. Без скина возвращается порядок аниматора/префаба.
    /// </summary>
    private void ApplySorting(bool skinned)
    {
        if (head == null) return;

        _headSorting.Refresh();
        foreach (var t in _transitionSorting) t.Refresh();

        if (!skinned)
        {
            _headSorting.Restore();
            foreach (var t in _transitionSorting) t.Restore();
            return;
        }

        int top = int.MinValue;
        foreach (var r in parts)
        {
            if (r == null || r == head || System.Array.IndexOf(transitionRenderers, r) >= 0) continue;
            if (r.sortingLayerID == head.sortingLayerID && r.sortingOrder > top) top = r.sortingOrder;
        }

        int headOrder = top == int.MinValue ? _headSorting.BaseOrder : Mathf.Max(_headSorting.BaseOrder, top + 1);
        _headSorting.Set(headOrder);

        // Transition'ы — над головой, сохраняя их взаимный порядок.
        for (int i = 0; i < _transitionSorting.Length; i++)
        {
            int rank = 1;
            for (int j = 0; j < _transitionSorting.Length; j++)
            {
                if (_transitionSorting[j].BaseOrder < _transitionSorting[i].BaseOrder) rank++;
            }
            _transitionSorting[i].Set(headOrder + rank);
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
