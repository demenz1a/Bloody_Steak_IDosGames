using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Настройка звуков: меню Bloody Steak/Setup Sounds (в открытой GameScene).
/// - SoundManager на Managers со ссылками на OrderManager, GameOverManager, MatchTimer
///   и строкой на каждый SoundId (клипы заполняются в инспекторе);
/// - станция Mincer (кухонная мясорубка) звучит Grinder, а не шипением;
/// - FootstepSounds на корне префаба игрока, AnimationSoundEvents на объектах с Animator
///   у игрока и клиента;
/// - Animation Events: KillRaise в начале WolfKillerKill, BodyFall в NPCMaleDead (в момент брызг крови,
///   при необходимости перетащите событие в окне Animation).
/// Повторный запуск ничего не дублирует. Batch: -executeMethod SoundSetup.SetupBatch.
/// </summary>
public static class SoundSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Test 1.prefab";
    private const string NpcPrefabPath = "Assets/Prefabs/NPCCustomer.prefab";
    private const string KillClipPath = "Assets/Animations/Player/WolfKillerKill.anim";
    private const string DeathClipPath = "Assets/Animations/NPCMale/NPCMaleDead.anim";

    private const string AudioFolder = "Assets/Audio";

    [MenuItem("Bloody Steak/Setup Sounds")]
    private static void SetupMenu() => Setup();

    [MenuItem("Bloody Steak/Setup Music")]
    private static void MusicMenu() => SetupMusic();

    public static void SetupMusicBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        SetupMusic();
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// MusicManager на Managers: слои из Assets/Audio/Music (Music_Level1_Chef / Level2_Wolf / Level3_Panic),
    /// ссылки на PlayerFormController и GameOverManager.
    /// </summary>
    private static void SetupMusic()
    {
        var orderManager = Object.FindFirstObjectByType<OrderManager>();
        var host = GameObject.Find("Managers") ?? orderManager?.gameObject;
        if (host == null)
        {
            Debug.LogError("Music: не найден объект Managers. Откройте GameScene.");
            return;
        }

        var music = host.GetComponent<MusicManager>() ?? Undo.AddComponent<MusicManager>(host);
        var so = new SerializedObject(music);
        so.FindProperty("chefLayer").objectReferenceValue = LoadClip("Music_Level1_Chef");
        so.FindProperty("wolfLayer").objectReferenceValue = LoadClip("Music_Level2_Wolf");
        so.FindProperty("panicLayer").objectReferenceValue = LoadClip("Music_Level3_Panic");
        so.FindProperty("playerForm").objectReferenceValue = Object.FindFirstObjectByType<PlayerFormController>();
        so.FindProperty("gameOver").objectReferenceValue = Object.FindFirstObjectByType<GameOverManager>();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(host.scene);
        Debug.Log("Music: MusicManager настроен на Managers.");
    }

    private static AudioClip LoadClip(string fileName) =>
        AssetDatabase.FindAssets($"{fileName} t:AudioClip", new[] { $"{AudioFolder}/Music" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .FirstOrDefault();

    [MenuItem("Bloody Steak/Assign Sounds From Folders")]
    private static void AssignMenu() => AssignClipsFromFolders();

    public static void AssignBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        AssignClipsFromFolders();
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Клипы каждого SoundId = все аудиофайлы в Assets/Audio/&lt;SoundId&gt;/ (например Assets/Audio/Footstep/).
    /// Строки без папки не трогаются, поэтому то, что назначено руками, не теряется.
    /// </summary>
    internal static void AssignClipsFromFolders()
    {
        var manager = Object.FindFirstObjectByType<SoundManager>();
        if (manager == null)
        {
            Debug.LogError("Sounds: в сцене нет SoundManager — сначала Bloody Steak/Setup Sounds.");
            return;
        }

        manager.EditorEnsureRows();
        var so = new SerializedObject(manager);
        var sounds = so.FindProperty("sounds");
        int assigned = 0;
        for (int i = 0; i < sounds.arraySize; i++)
        {
            var entry = sounds.GetArrayElementAtIndex(i);
            var id = (SoundId)entry.FindPropertyRelative("id").enumValueIndex;
            string folder = $"{AudioFolder}/{id}";
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(c => c != null)
                .ToArray();
            if (clips.Length == 0) continue;

            var prop = entry.FindPropertyRelative("clips");
            prop.arraySize = clips.Length;
            for (int c = 0; c < clips.Length; c++) prop.GetArrayElementAtIndex(c).objectReferenceValue = clips[c];
            assigned++;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log($"Sounds: клипы назначены для {assigned} звуков из {AudioFolder}/<SoundId>/.");
    }

    public static void SetupBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Setup();
        EditorSceneManager.SaveScene(scene);
    }

    private static void Setup()
    {
        SetupScene();
        SetupPrefabs();
        AddEvent(KillClipPath, "KillRaise", 0f);
        AddEvent(DeathClipPath, "BodyFall", FindEventTime(DeathClipPath, "PlayCookingParticles", 0.8333f));
        AssetDatabase.SaveAssets();
        Debug.Log("Sounds: настройка завершена. Клипы назначаются в инспекторе SoundManager (объект Managers).");
    }

    private static void SetupScene()
    {
        var orderManager = Object.FindFirstObjectByType<OrderManager>();
        var host = GameObject.Find("Managers") ?? orderManager?.gameObject;
        if (host == null)
        {
            Debug.LogError("Sounds: не найден объект Managers. Откройте GameScene.");
            return;
        }

        var manager = host.GetComponent<SoundManager>();
        bool created = manager == null;
        if (created) manager = Undo.AddComponent<SoundManager>(host);
        if (created) manager.EditorResetDefaults();

        var so = new SerializedObject(manager);
        so.FindProperty("orderManager").objectReferenceValue = orderManager;
        so.FindProperty("gameOver").objectReferenceValue = Object.FindFirstObjectByType<GameOverManager>();
        so.FindProperty("matchTimer").objectReferenceValue = Object.FindFirstObjectByType<MatchTimer>();
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        foreach (var station in Object.FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
        {
            if (station.name != "Mincer") continue;
            var sso = new SerializedObject(station);
            sso.FindProperty("cookingLoop").enumValueIndex = (int)SoundId.Grinder;
            sso.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(host.scene);
    }

    private static void SetupPrefabs()
    {
        EditPrefab(PlayerPrefabPath, root =>
        {
            var steps = root.GetComponent<FootstepSounds>() ?? root.AddComponent<FootstepSounds>();
            var so = new SerializedObject(steps);
            so.FindProperty("movement").objectReferenceValue = root.GetComponentInChildren<PlayerMovement>(true);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddEventReceivers(root);
        });
        EditPrefab(NpcPrefabPath, AddEventReceivers);
    }

    /// <summary>Animation Events вызываются на объекте с Animator — туда и вешается приёмник.</summary>
    private static void AddEventReceivers(GameObject root)
    {
        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator.GetComponent<AnimationSoundEvents>() == null)
                animator.gameObject.AddComponent<AnimationSoundEvents>();
        }
    }

    private static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static float FindEventTime(string clipPath, string functionName, float fallback)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) return fallback;
        var ev = AnimationUtility.GetAnimationEvents(clip).FirstOrDefault(e => e.functionName == functionName);
        return ev != null ? ev.time : fallback;
    }

    private static void AddEvent(string clipPath, string soundId, float time)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"Sounds: нет анимации {clipPath}.");
            return;
        }

        var events = AnimationUtility.GetAnimationEvents(clip).ToList();
        if (events.Any(e => e.functionName == nameof(AnimationSoundEvents.PlaySound) && e.stringParameter == soundId)) return;

        events.Add(new AnimationEvent
        {
            time = time,
            functionName = nameof(AnimationSoundEvents.PlaySound),
            stringParameter = soundId,
        });
        AnimationUtility.SetAnimationEvents(clip, events.OrderBy(e => e.time).ToArray());
        EditorUtility.SetDirty(clip);
    }
}
