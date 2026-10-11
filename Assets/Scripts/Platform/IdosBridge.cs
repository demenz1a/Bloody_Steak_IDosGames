using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Мост между игрой и веб-оболочкой iDosGames (проект IdosWeb, модуль bloody-steak).
/// Оболочка отвечает за вход игрока и лидерборд; игра только сообщает ей итоговый счёт.
///
/// C# → JS: SubmitScore вызывает функцию из Assets/Plugins/WebGL/IdosBridge.jslib, которая
/// передаёт счёт в window.BloodySteakHost. Вне оболочки (редактор, билд без iDos) — ничего не делает.
///
/// JS → C#: оболочка ставит игру на паузу, когда игрок уходит в лобби, через
/// SendMessage("IdosBridge", "OnHostSuspend" / "OnHostResume") и надевает купленный скин через
/// SendMessage("IdosBridge", "OnHostSetSkin", skinId). Для этого объект с этим именем
/// создаётся сам при старте и живёт между сценами.
/// </summary>
public class IdosBridge : MonoBehaviour
{
    private const string ObjectName = "IdosBridge";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void IdosBridge_SubmitScore(int score);
    [DllImport("__Internal")] private static extern void IdosBridge_ReportVictory(int coins);
#endif

    private float _timeScaleBeforeSuspend = 1f;
    private bool _suspended;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        var go = new GameObject(ObjectName);
        go.AddComponent<IdosBridge>();
        DontDestroyOnLoad(go);
    }

    /// <summary>Отправить итоговый счёт смены в лидерборд iDosGames.</summary>
    public static void SubmitScore(int score)
    {
        Debug.Log($"[IdosBridge] SubmitScore({score})");
#if UNITY_WEBGL && !UNITY_EDITOR
        IdosBridge_SubmitScore(score);
#endif
    }

    /// <summary>
    /// Смена выиграна, заработано coins BloodyCoin: оболочка набирает эту сумму наградами-номиналами
    /// shift_coin_1..32. Сервер ограничивает каждую номиналом-награду по частоте — игра ничего не начисляет.
    /// </summary>
    public static void ReportVictory(int coins)
    {
        Debug.Log($"[IdosBridge] ReportVictory({coins})");
#if UNITY_WEBGL && !UNITY_EDITOR
        IdosBridge_ReportVictory(coins);
#endif
    }

    // ---- Вызываются из JS через SendMessage ----

    private void OnHostSuspend()
    {
        if (_suspended) return;

        _suspended = true;
        _timeScaleBeforeSuspend = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    /// <summary>Оболочка прислала надетый скин (id из IdosWeb/src/shared/skins.ts; "default" — без скина).</summary>
    private void OnHostSetSkin(string skinId) => PlayerSkinSelection.Set(skinId);

    private void OnHostResume()
    {
        if (!_suspended) return;

        _suspended = false;
        // Если за время паузы сцену перезагрузили (рестарт выставляет 1), не затираем это.
        if (Time.timeScale == 0f) Time.timeScale = _timeScaleBeforeSuspend;
        AudioListener.pause = false;
    }
}
