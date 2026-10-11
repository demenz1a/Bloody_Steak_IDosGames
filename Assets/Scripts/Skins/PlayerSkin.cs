using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Один скин игрока: какие спрайты базовой модельки чем заменить. Анимации не трогаются —
/// аниматор продолжает ставить базовые спрайты, а PlayerSkinApplier после него подменяет их
/// по этой таблице. Поэтому скин работает со всеми текущими и будущими анимациями.
///
/// swaps — замены всегда (голова, тело, руки, ноги, ступни, таз, хвост, голова и тело повара).
/// chefSwaps — замены поверх swaps, пока на игроке форма повара: руки повара и голова/тело повара,
/// нарезанные по прямоугольникам волка (со скином форма повара играет анимации волка).
///
/// Ассет собирается пунктом меню Bloody Steak/Import Player Skin (Editor/PlayerSkinImporter.cs).
/// skinId совпадает с id скина на стороне iDosGames (IdosWeb/src/shared/skins.ts).
/// </summary>
[CreateAssetMenu(menuName = "Bloody Steak/Player Skin", fileName = "PlayerSkin")]
public class PlayerSkin : ScriptableObject
{
    [Serializable]
    public struct SpriteSwap
    {
        public Sprite original;
        public Sprite replacement;
    }

    [SerializeField] private string skinId;
    [SerializeField] private string displayName;
    [SerializeField] private SpriteSwap[] swaps = Array.Empty<SpriteSwap>();
    [SerializeField] private SpriteSwap[] chefSwaps = Array.Empty<SpriteSwap>();
    // Со скином стойка и ходьба повара — волчьи (PlayerSkinApplier), но анимации смены формы всё ещё
    // показывают кадры в позе повара: Chest и руки стоят под базовое тело повара (другой прямоугольник
    // спрайта, pivot ниже на ~250 px холста, руки подняты). На эти кадры сдвиги ниже (локальные единицы,
    // текстуры ужаты до 2048, т.е. 1 ед. ≈ 127 px холста) возвращают взаимное расположение формы волка.
    [Tooltip("Сдвиг головы в обеих формах, в локальных единицах (например, поднять голову скина повыше).")]
    [SerializeField] private Vector2 headOffset = Vector2.zero;

    [Header("Форма повара: сдвиги поверх анимации")]
    [Tooltip("Сдвиг Chest (вместе с руками).")]
    [SerializeField] private Vector2 chefChestOffset = new Vector2(0.57f, -0.74f);
    [Tooltip("Дополнительный сдвиг левой руки относительно Chest.")]
    [SerializeField] private Vector2 chefLeftArmOffset = new Vector2(-0.51f, 0.65f);
    [Tooltip("Дополнительный сдвиг правой руки относительно Chest.")]
    [SerializeField] private Vector2 chefRightArmOffset = new Vector2(-0.30f, 1.04f);

    private Dictionary<Sprite, Sprite> _map;
    private Dictionary<Sprite, Sprite> _chefMap;

    public string SkinId => skinId;
    public string DisplayName => displayName;
    public Vector2 HeadOffset => headOffset;
    public Vector2 ChefChestOffset => chefChestOffset;
    public Vector2 ChefLeftArmOffset => chefLeftArmOffset;
    public Vector2 ChefRightArmOffset => chefRightArmOffset;

    /// <summary>Чем заменить базовый спрайт; null — оставить как есть.</summary>
    public Sprite Resolve(Sprite original, bool chefForm)
    {
        if (original == null) return null;
        if (_map == null) BuildMaps();

        if (chefForm && _chefMap.TryGetValue(original, out var chef)) return chef;
        return _map.TryGetValue(original, out var sprite) ? sprite : null;
    }

    private void BuildMaps()
    {
        _map = ToMap(swaps);
        _chefMap = ToMap(chefSwaps);
    }

    private static Dictionary<Sprite, Sprite> ToMap(SpriteSwap[] list)
    {
        var map = new Dictionary<Sprite, Sprite>();
        if (list == null) return map;

        foreach (var swap in list)
        {
            if (swap.original != null && swap.replacement != null) map[swap.original] = swap.replacement;
        }
        return map;
    }

    private void OnValidate() => _map = null; // правки в инспекторе — пересобрать таблицы

#if UNITY_EDITOR
    /// <summary>Для импортёра скинов.</summary>
    public void EditorSet(string id, string title, SpriteSwap[] always, SpriteSwap[] chef)
    {
        skinId = id;
        displayName = title;
        swaps = always;
        chefSwaps = chef;
        _map = null;
    }
#endif
}
