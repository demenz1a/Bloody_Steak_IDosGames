using System;
using UnityEngine;

/// <summary>
/// Какой скин сейчас надет. Источник — оболочка iDosGames: она знает, какие скины куплены,
/// и присылает выбранный через IdosBridge.OnHostSetSkin. Значение запоминается в PlayerPrefs,
/// чтобы скин не слетал при перезагрузке сцены (рестарт, выход в меню).
/// Право владения здесь не проверяется — это делает оболочка до отправки.
/// </summary>
public static class PlayerSkinSelection
{
    public const string DefaultSkinId = "default";
    private const string PrefsKey = "player_skin";

    private static string _current;

    public static event Action<string> Changed;

    public static string Current
    {
        get
        {
            if (_current == null) _current = PlayerPrefs.GetString(PrefsKey, DefaultSkinId);
            return _current;
        }
    }

    public static void Set(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) skinId = DefaultSkinId;
        if (skinId == Current) return;

        _current = skinId;
        PlayerPrefs.SetString(PrefsKey, skinId);
        Changed?.Invoke(skinId);
    }
}
