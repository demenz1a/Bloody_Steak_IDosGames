using UnityEngine;

/// <summary>
/// Музыка смены из трёх слоёв одной композиции, от спокойного к напряжённому:
/// 1 — игрок в форме повара, 2 — в форме волка, 3 — кто-то паникует (перебивает форму).
///
/// Все три слоя играют ОДНОВРЕМЕННО и синхронно, слышен только нужный — переключение
/// это кроссфейд громкости, а не перезапуск трека, поэтому для игрока оно бесшовное.
///
/// Синхронность: слой 1 зациклен сам и задаёт время. Слои 2 и 3 не зацикливаются, а
/// перезапускаются по аудиочасам (AudioSettings.dspTime, PlayScheduled) ровно в момент
/// перезапуска слоя 1. Так расхождение не накапливается, даже если длины файлов отличаются
/// на доли секунды (у mp3 так бывает из-за кодирования). Для бесшовного перезапуска у каждого
/// такого слоя два источника, которые работают по очереди.
///
/// Пауза из лобби iDos (AudioListener.pause) замораживает источники, но не аудиочасы, поэтому
/// после неё слои 2 и 3 заново выравниваются по позиции слоя 1.
/// После победы музыка плавно затихает (GameOverManager.OnVictory), после поражения продолжает
/// играть (keepMusicOnDefeat).
/// </summary>
public class MusicManager : MonoBehaviour
{
    [Header("Layers (одинаковый темп и длина)")]
    [SerializeField] private AudioClip chefLayer;
    [SerializeField] private AudioClip wolfLayer;
    [SerializeField] private AudioClip panicLayer;

    [Header("Sources")]
    [SerializeField] private PlayerFormController playerForm;
    [SerializeField] private GameOverManager gameOver;

    [Header("Mix")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;
    [Tooltip("Длительность кроссфейда между слоями, сек.")]
    [SerializeField] private float crossfadeTime = 0.6f;
    [Tooltip("Затухание в конце игры, сек.")]
    [SerializeField] private float endFadeTime = 1.5f;
    [Tooltip("На экране поражения музыка продолжает играть. Выключите, чтобы она затихала, как после победы.")]
    [SerializeField] private bool keepMusicOnDefeat = true;

    private const double StartDelay = 0.2;      // запас, чтобы все слои успели стартовать в один момент
    private const double ScheduleAhead = 0.5;   // за сколько до конца цикла планировать следующий

    private AudioSource _master;
    private readonly AudioSource[][] _followers = new AudioSource[2][]; // [слой 2/3][A/B]
    private readonly int[] _activeFollower = new int[2];
    private readonly float[] _layerVolume = new float[3];

    private double _cycleStartDsp;
    private double _cycleLength;
    private bool _nextScheduled;
    private bool _wasPaused;
    private bool _ended;

    private void Awake()
    {
        if (chefLayer == null) { enabled = false; return; }

        _master = CreateSource("Music Chef", chefLayer, loop: true);
        _followers[0] = new[] { CreateSource("Music Wolf A", wolfLayer), CreateSource("Music Wolf B", wolfLayer) };
        _followers[1] = new[] { CreateSource("Music Panic A", panicLayer), CreateSource("Music Panic B", panicLayer) };
        _cycleLength = (double)chefLayer.samples / chefLayer.frequency;
    }

    private void OnEnable()
    {
        if (gameOver == null) return;
        gameOver.OnGameOver += HandleGameOver;
        gameOver.OnVictory += HandleVictory;
    }

    private void OnDisable()
    {
        if (gameOver == null) return;
        gameOver.OnGameOver -= HandleGameOver;
        gameOver.OnVictory -= HandleVictory;
    }

    private void HandleGameOver(GameOverReason reason) => _ended = !keepMusicOnDefeat;
    private void HandleVictory() => _ended = true;

    private void Start()
    {
        double start = AudioSettings.dspTime + StartDelay;
        _master.PlayScheduled(start);
        for (int i = 0; i < 2; i++)
        {
            _activeFollower[i] = 0;
            _followers[i][0].PlayScheduled(start);
        }
        _cycleStartDsp = start;
        _nextScheduled = false;
    }

    private void Update()
    {
        UpdateVolumes();

        bool paused = AudioListener.pause;
        if (_wasPaused && !paused) Resync();
        _wasPaused = paused;
        if (paused) return;

        double now = AudioSettings.dspTime;
        double nextCycle = _cycleStartDsp + _cycleLength;

        // Заранее ставим следующий цикл слоёв 2 и 3 на тот же момент, когда слой 1 начнётся заново.
        if (!_nextScheduled && now >= nextCycle - ScheduleAhead)
        {
            for (int i = 0; i < 2; i++)
            {
                // Текущий источник обрывается ровно на стыке (если файл слоя чуть длиннее слоя 1).
                _followers[i][_activeFollower[i]].SetScheduledEndTime(nextCycle);

                int next = 1 - _activeFollower[i];
                var source = _followers[i][next];
                source.Stop();
                source.time = 0f;
                source.PlayScheduled(nextCycle);
                _activeFollower[i] = next;
            }
            _nextScheduled = true;
        }

        if (now >= nextCycle)
        {
            _cycleStartDsp = nextCycle;
            _nextScheduled = false;
        }
    }

    /// <summary>После паузы: слои 2 и 3 ставятся на позицию слоя 1, цикл пересчитывается от сейчас.</summary>
    private void Resync()
    {
        float position = _master.time;
        double now = AudioSettings.dspTime;
        double start = now + StartDelay;
        _cycleStartDsp = now - position; // слой 1 уже играет с позиции position
        _nextScheduled = false;

        for (int i = 0; i < 2; i++)
        {
            foreach (var s in _followers[i]) s.Stop();
            var source = _followers[i][0];
            _activeFollower[i] = 0;
            float from = position + (float)StartDelay; // где будет слой 1 к моменту старта
            if (source.clip == null || from >= source.clip.length) continue; // слой короче — подхватит следующий цикл
            source.time = from;
            source.PlayScheduled(start);
        }
    }

    private void UpdateVolumes()
    {
        int target = CurrentLayer();
        float fade = _ended ? endFadeTime : crossfadeTime;
        float step = fade > 0f ? Time.unscaledDeltaTime / fade : 1f;

        for (int layer = 0; layer < 3; layer++)
        {
            float goal = !_ended && layer == target ? 1f : 0f;
            _layerVolume[layer] = Mathf.MoveTowards(_layerVolume[layer], goal, step);
        }

        _master.volume = _layerVolume[0] * volume;
        for (int i = 0; i < 2; i++)
        {
            foreach (var s in _followers[i]) s.volume = _layerVolume[i + 1] * volume;
        }
    }

    /// <summary>0 — повар, 1 — волк, 2 — паника.</summary>
    private int CurrentLayer()
    {
        if (PanicManager.Instance != null && PanicManager.Instance.IsPanicActive && panicLayer != null) return 2;
        if (playerForm != null && playerForm.IsWolf && wolfLayer != null) return 1;
        return 0;
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop = false)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.priority = 0; // музыка не должна вытесняться звуками
        return source;
    }
}
