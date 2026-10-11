using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Все звуки игры. Клип(ы) для каждого задаются в инспекторе SoundManager.</summary>
public enum SoundId
{
    Sizzle,          // шипение во время готовки (петля, CookingStation)
    Grinder,         // переработка трупа в мясорубке (петля, MeatGrinderMachine)
    Footstep,        // шаг игрока (FootstepSounds или Animation Event)
    SheepBleat,      // клиент сделал заказ (OrderManager.OnOrderCreated)
    KillRaise,       // взмах кинжала (Animation Event в WolfKillerKill)
    BodyFall,        // тело падает на пол (Animation Event в NPCMaleDead)
    Trash,           // предмет выброшен в мусорку (TrashBin)
    AttemptLost,     // провален заказ, сгорела попытка (OrderManager.OnAttemptsChanged)
    Defeat,          // окончательный проигрыш (GameOverManager.OnGameOver)
    Victory,         // победа (GameOverManager.OnVictory)
    ClockTick,       // тиканье часов в конце смены (петля, MatchTimer)
    OrderDelivered,  // заказ выдан (OrderManager.OnOrderCompleted)
    FoodReady,       // еда приготовилась (CookingStation)
    FoodBurnt,       // еда сгорела (CookingStation)
    Spotted,         // игрока заметили (PanicManager.OnPanicStarted)
    // Новые звуки — только в конец: id хранится в сцене числом.
    DoorBell,        // клиент вошёл (NpcSpawner)
    ToiletFlush,     // клиент вышел из туалета (NpcCustomer)
    NpcDeathScream,  // предсмертный крик барана (NpcCustomer.Kill)
    LightSwitch      // щелчок рубильника (LightSwitch)
}

/// <summary>
/// Единственная точка воспроизведения звуков. Синглтон (Instance), как PanicManager:
/// станции, мусорка и Animation Events зовут статические Play/StartLoop/StopLoop, а события
/// заказов, конца игры, таймера и паники менеджер слушает сам.
///
/// Короткие звуки играются из небольшого пула AudioSource (с лёгким разбросом высоты, чтобы
/// повторы не звучали одинаково), петли — из отдельного источника на владельца (станцию и т.п.).
/// Пустой клип — тишина без ошибок, поэтому звуки можно заполнять постепенно.
///
/// После победы все петли глушатся, после поражения продолжают играть (keepSoundsOnDefeat);
/// звук победы/поражения играется на unscaled time
/// (AudioSource от Time.timeScale не зависит). Пауза из лобби iDos — AudioListener.pause в IdosBridge.
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        [Tooltip("Если клипов несколько — каждый раз берётся случайный.")]
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Случайный разброс высоты ±, чтобы повторы не звучали одинаково (только для коротких звуков).")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.05f;
        [Tooltip("Минимальная пауза между повторами (сек): защита от наложения, если событие пришло несколько раз подряд.")]
        public float minInterval;
    }

    public static SoundManager Instance { get; private set; }

    [SerializeField] private SoundEntry[] sounds = Array.Empty<SoundEntry>();

    [Header("Sources")]
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private GameOverManager gameOver;
    [SerializeField] private MatchTimer matchTimer;

    [Header("Game over")]
    [Tooltip("На экране поражения звуки продолжают играть (петли станций, тиканье). Выключите, чтобы глушить их.")]
    [SerializeField] private bool keepSoundsOnDefeat = true;

    [Header("Clock")]
    [Tooltip("За сколько секунд до конца смены начинают тикать часы (совпадает с покраснением таймера).")]
    [SerializeField] private float clockTickSeconds = 30f;

    [Header("Pool")]
    [SerializeField] private int oneShotVoices = 8;

    private readonly Dictionary<SoundId, SoundEntry> _byId = new Dictionary<SoundId, SoundEntry>();
    private readonly Dictionary<SoundId, float> _lastPlayed = new Dictionary<SoundId, float>();
    private readonly Dictionary<object, AudioSource> _loops = new Dictionary<object, AudioSource>();
    private readonly object _clockOwner = new object();
    private AudioSource[] _voices;
    private int _nextVoice;

    // ---- Статический доступ для систем, которые не держат ссылку на менеджер ----

    public static void Play(SoundId id) => Instance?.PlayOneShot(id);
    public static void StartLoop(SoundId id, object owner) => Instance?.StartLoopFor(id, owner);
    public static void StopLoop(object owner) => Instance?.StopLoopFor(owner);

    private void Awake()
    {
        Instance = this;
        foreach (var entry in sounds)
        {
            if (entry != null) _byId[entry.id] = entry;
        }

        _voices = new AudioSource[Mathf.Max(1, oneShotVoices)];
        for (int i = 0; i < _voices.Length; i++) _voices[i] = CreateSource("Voice " + i);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (orderManager != null)
        {
            orderManager.OnOrderCreated += HandleOrderCreated;
            orderManager.OnOrderCompleted += HandleOrderCompleted;
            orderManager.OnAttemptsChanged += HandleAttemptsChanged;
        }
        if (gameOver != null)
        {
            gameOver.OnGameOver += HandleGameOver;
            gameOver.OnVictory += HandleVictory;
        }
        // PanicManager живёт на той же сцене; Awake всех объектов уже прошёл к Start, поэтому подписка в Start.
    }

    private void Start()
    {
        if (PanicManager.Instance != null) PanicManager.Instance.OnPanicStarted += HandlePanicStarted;
    }

    private void OnDisable()
    {
        if (orderManager != null)
        {
            orderManager.OnOrderCreated -= HandleOrderCreated;
            orderManager.OnOrderCompleted -= HandleOrderCompleted;
            orderManager.OnAttemptsChanged -= HandleAttemptsChanged;
        }
        if (gameOver != null)
        {
            gameOver.OnGameOver -= HandleGameOver;
            gameOver.OnVictory -= HandleVictory;
        }
        if (PanicManager.Instance != null) PanicManager.Instance.OnPanicStarted -= HandlePanicStarted;
    }

    private void Update()
    {
        // При поражении (если keepSoundsOnDefeat) часы продолжают тикать; после победы — нет.
        bool stopped = gameOver != null && (keepSoundsOnDefeat ? gameOver.IsVictory : gameOver.IsGameOver);
        bool tick = matchTimer != null
            && !stopped
            && matchTimer.Remaining > 0f
            && matchTimer.Remaining <= clockTickSeconds;

        if (tick) StartLoopFor(SoundId.ClockTick, _clockOwner);
        else StopLoopFor(_clockOwner);
    }

    // ---- События игры ----

    private void HandleOrderCreated(Order order) => PlayOneShot(SoundId.SheepBleat);
    private void HandleOrderCompleted(Order order) => PlayOneShot(SoundId.OrderDelivered);

    private void HandleAttemptsChanged(int remaining, int max)
    {
        // Последняя попытка — это уже поражение, его звук играет HandleGameOver.
        if (remaining > 0) PlayOneShot(SoundId.AttemptLost);
    }

    private void HandleGameOver(GameOverReason reason)
    {
        if (!keepSoundsOnDefeat) StopAllLoops();
        PlayOneShot(SoundId.Defeat);
    }

    private void HandleVictory()
    {
        StopAllLoops();
        PlayOneShot(SoundId.Victory);
    }

    private void HandlePanicStarted(NpcCustomer npc, PanicSeverity severity) => PlayOneShot(SoundId.Spotted);

    // ---- Воспроизведение ----

    private void PlayOneShot(SoundId id)
    {
        if (!TryGetClip(id, out var entry, out var clip)) return;

        if (entry.minInterval > 0f)
        {
            if (_lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < entry.minInterval) return;
            _lastPlayed[id] = Time.unscaledTime;
        }

        var voice = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % _voices.Length;

        voice.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);
        voice.PlayOneShot(clip, entry.volume);
    }

    private void StartLoopFor(SoundId id, object owner)
    {
        if (owner == null) return;
        if (_loops.TryGetValue(owner, out var existing) && existing != null && existing.isPlaying) return;
        if (!TryGetClip(id, out var entry, out var clip)) return;

        if (existing == null)
        {
            existing = CreateSource("Loop " + id);
            _loops[owner] = existing;
        }
        existing.clip = clip;
        existing.volume = entry.volume;
        existing.loop = true;
        existing.pitch = 1f;
        existing.Play();
    }

    private void StopLoopFor(object owner)
    {
        if (owner != null && _loops.TryGetValue(owner, out var source) && source != null) source.Stop();
    }

    private void StopAllLoops()
    {
        foreach (var source in _loops.Values)
        {
            if (source != null) source.Stop();
        }
    }

    private bool TryGetClip(SoundId id, out SoundEntry entry, out AudioClip clip)
    {
        clip = null;
        if (!_byId.TryGetValue(id, out entry) || entry.clips == null || entry.clips.Length == 0) return false;

        clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
        return clip != null;
    }

    private AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D-игра: звук не зависит от положения камеры
        return source;
    }

    /// <summary>В инспекторе всегда есть строка на каждый SoundId — новые звуки появляются сами.</summary>
    private void OnValidate()
    {
        var ids = (SoundId[])Enum.GetValues(typeof(SoundId));
        var list = new List<SoundEntry>(sounds ?? Array.Empty<SoundEntry>());
        foreach (var id in ids)
        {
            if (!list.Exists(e => e != null && e.id == id)) list.Add(new SoundEntry { id = id });
        }
        list.Sort((a, b) => a.id.CompareTo(b.id));
        if (list.Count != (sounds?.Length ?? 0)) sounds = list.ToArray();
    }

#if UNITY_EDITOR
    /// <summary>Добавить строки для SoundId, которых ещё нет в инспекторе (после расширения enum).</summary>
    public void EditorEnsureRows() => OnValidate();

    /// <summary>Для VictorySetup-подобной настройки сцены: заполнить строки и разумные значения по умолчанию.</summary>
    public void EditorResetDefaults()
    {
        sounds = null;
        OnValidate();
        foreach (var e in sounds)
        {
            switch (e.id)
            {
                case SoundId.Sizzle:
                case SoundId.Grinder:
                case SoundId.ClockTick:
                    e.pitchJitter = 0f;
                    e.volume = 0.6f;
                    break;
                case SoundId.Footstep:
                    e.volume = 0.5f;
                    e.pitchJitter = 0.1f;
                    break;
                case SoundId.Spotted:
                    e.minInterval = 0.5f;
                    break;
                case SoundId.SheepBleat:
                    e.minInterval = 0.3f;
                    e.pitchJitter = 0.12f;
                    break;
                case SoundId.Defeat:
                case SoundId.Victory:
                    e.pitchJitter = 0f;
                    break;
            }
        }
    }
#endif
}
