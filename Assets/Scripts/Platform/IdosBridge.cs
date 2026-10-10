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
/// SendMessage("IdosBridge", "OnHostSuspend" / "OnHostResume"). Для этого объект с этим именем
/// создаётся сам при старте и живёт между сценами.
/// </summary>
public class IdosBridge : MonoBehaviour
{
    private const string ObjectName = "IdosBridge";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void IdosBridge_SubmitScore(int score);
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

    // ---- Вызываются из JS через SendMessage ----

    private void OnHostSuspend()
    {
        if (_suspended) return;

        _suspended = true;
        _timeScaleBeforeSuspend = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void OnHostResume()
    {
        if (!_suspended) return;

        _suspended = false;
        // Если за время паузы сцену перезагрузили (рестарт выставляет 1), не затираем это.
        if (Time.timeScale == 0f) Time.timeScale = _timeScaleBeforeSuspend;
        AudioListener.pause = false;
    }
}
