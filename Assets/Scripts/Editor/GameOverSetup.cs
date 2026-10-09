using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Одноразовая настройка системы геймовера в открытой GameScene: меню Bloody Steak/Setup Game Over.
/// Создаёт GameOverManager (на Managers), AttemptsUI (на Lifes), экран GameOverScreen
/// в GameUICanvas и добавляет текущую сцену в Build Settings (без этого не работает рестарт).
/// Повторный запуск ничего не дублирует: уже существующие объекты и компоненты переиспользуются.
/// </summary>
public static class GameOverSetup
{
    private const string ScreenName = "GameOverScreen";

    [MenuItem("Bloody Steak/Setup Game Over")]
    private static void Setup()
    {
        var orderManager = Object.FindFirstObjectByType<OrderManager>();
        var canvas = FindByName<Canvas>("GameUICanvas");
        if (orderManager == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Game Over Setup",
                "Не найдены OrderManager или GameUICanvas. Откройте GameScene и запустите снова.", "OK");
            return;
        }

        var manager = SetupManager(orderManager);
        SetupAttempts(orderManager);
        SetupScreen(canvas, manager);
        AddSceneToBuildSettings();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log("Game Over: настройка завершена. Сохраните сцену (Ctrl+S).");
    }

    private static GameOverManager SetupManager(OrderManager orderManager)
    {
        var host = GameObject.Find("Managers") ?? orderManager.gameObject;
        var manager = GetOrAdd<GameOverManager>(host);

        var so = new SerializedObject(manager);
        so.FindProperty("orderManager").objectReferenceValue = orderManager;
        so.FindProperty("playerInteractor").objectReferenceValue = Object.FindFirstObjectByType<PlayerInteractor>();
        so.FindProperty("playerMovement").objectReferenceValue = Object.FindFirstObjectByType<PlayerMovement>();
        so.ApplyModifiedProperties();
        return manager;
    }

    private static void SetupAttempts(OrderManager orderManager)
    {
        var lifes = GameObject.Find("Lifes");
        if (lifes == null)
        {
            Debug.LogWarning("Game Over: объект Lifes не найден — назначьте иконки в AttemptsUI вручную.");
            return;
        }

        var attempts = GetOrAdd<AttemptsUI>(lifes);
        var images = lifes.GetComponentsInChildren<Image>(true);

        var so = new SerializedObject(attempts);
        so.FindProperty("orderManager").objectReferenceValue = orderManager;
        var iconsProp = so.FindProperty("icons");
        iconsProp.arraySize = images.Length;
        for (int i = 0; i < images.Length; i++)
        {
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        }
        so.ApplyModifiedProperties();
    }

    private static void SetupScreen(Canvas canvas, GameOverManager manager)
    {
        var existing = canvas.transform.Find(ScreenName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // Хост с GameOverUI всегда активен, а root (сам экран) выключается до геймовера.
        var host = CreateUIObject(ScreenName, canvas.transform);
        Stretch(host);
        host.transform.SetAsLastSibling(); // поверх остального HUD

        var root = CreateUIObject("Root", host.transform);
        Stretch(root);

        var blood = CreateUIObject("BloodOverlay", root.transform);
        Stretch(blood);
        var bloodImage = blood.AddComponent<Image>();
        bloodImage.color = new Color(0.45f, 0f, 0f, 0.95f);

        var panel = CreateUIObject("Panel", root.transform);
        Stretch(panel);
        var panelGroup = panel.AddComponent<CanvasGroup>();

        var title = CreateText("Title", panel.transform, "GAME OVER", 120);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1200, 200));

        var restart = CreateButton("RestartButton", panel.transform, "Начать заново");
        SetRect((RectTransform)restart.transform, new Vector2(0.5f, 0.42f), new Vector2(420, 90));

        var menu = CreateButton("MainMenuButton", panel.transform, "Выход в меню");
        SetRect((RectTransform)menu.transform, new Vector2(0.5f, 0.30f), new Vector2(420, 90));

        var ui = host.AddComponent<GameOverUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("manager").objectReferenceValue = manager;
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("bloodOverlay").objectReferenceValue = blood.transform;
        so.FindProperty("panel").objectReferenceValue = panelGroup;
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.ApplyModifiedProperties();

        root.SetActive(false);
    }

    private static void AddSceneToBuildSettings()
    {
        var path = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(path)) return;

        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"Game Over: {path} добавлена в Build Settings.");
    }

    // ---- helpers ----

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static T FindByName<T>(string objectName) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == objectName);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size)
    {
        var go = CreateUIObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var go = CreateUIObject(name, parent);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText("Label", go.transform, label, 42);
        Stretch(text.gameObject);
        return button;
    }
}
