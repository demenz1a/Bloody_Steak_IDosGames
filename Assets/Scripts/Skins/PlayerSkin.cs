using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Один скин игрока: какие спрайты базовой модельки чем заменить. Анимации не трогаются —
/// аниматор продолжает ставить базовые спрайты, а PlayerSkinApplier после него подменяет их
/// по этой таблице. Поэтому скин работает со всеми текущими и будущими анимациями.
///
/// swaps — замены всегда (голова, тело, руки, ноги, ступни, таз, хвост, голова и тело повара).
/// chefSwaps — замены поверх swaps, пока на игроке форма повара (например, свои руки повара).
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
    [Tooltip("Сдвиг тела (Chest вместе с руками) в форме повара, в локальных единицах. Тело повара в скинах " +
             "нарисовано на месте обычного тела, а базовое тело повара — ниже (~126 px холста ≈ 1 ед.: текстуры " +
             "ужаты до 2048), поэтому без сдвига скин в форме повара сидит слишком высоко.")]
    [SerializeField] private Vector2 chefBodyOffset = new Vector2(0f, -1f);

    private Dictionary<Sprite, Sprite> _map;
    private Dictionary<Sprite, Sprite> _chefMap;

    public string SkinId => skinId;
    public string DisplayName => displayName;
    public Vector2 ChefBodyOffset => chefBodyOffset;

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
