using UnityEngine;

/// <summary>
/// Список всех скинов игрока. Лежит в Resources (Assets/Resources/PlayerSkinCatalog.asset),
/// чтобы PlayerSkinApplier находил его без ссылок в сцене. Новый скин добавляется сюда
/// импортёром (Bloody Steak/Import Player Skin). Базовый вид — это отсутствие скина
/// (id "default"), отдельного ассета для него нет.
/// </summary>
[CreateAssetMenu(menuName = "Bloody Steak/Player Skin Catalog", fileName = "PlayerSkinCatalog")]
public class PlayerSkinCatalog : ScriptableObject
{
    public const string ResourcePath = "PlayerSkinCatalog";

    [SerializeField] private PlayerSkin[] skins = new PlayerSkin[0];

    public PlayerSkin[] Skins => skins;

    private static PlayerSkinCatalog _instance;

    public static PlayerSkinCatalog Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<PlayerSkinCatalog>(ResourcePath);
            return _instance;
        }
    }

    /// <summary>Скин по id; null для "default" и неизвестных id.</summary>
    public PlayerSkin Find(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return null;

        foreach (var skin in skins)
        {
            if (skin != null && skin.SkinId == skinId) return skin;
        }
        return null;
    }

#if UNITY_EDITOR
    public void EditorAdd(PlayerSkin skin)
    {
        foreach (var s in skins)
        {
            if (s == skin) return;
        }
        var list = new System.Collections.Generic.List<PlayerSkin>(skins) { skin };
        skins = list.ToArray();
    }
#endif
}
