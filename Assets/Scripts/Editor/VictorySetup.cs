using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Настройка конца смены, денег и очков в открытой GameScene: меню Bloody Steak/Setup Victory & Economy.
/// Добавляет на Managers MatchTimer, MoneyManager и ScoreManager, связывает их с GameOverManager
/// и DifficultyManager, создаёт в GameUICanvas таймер, деньги и экран победы VictoryScreen.
/// Требует уже выполненных Setup Game Over и Setup Difficulty.
/// Повторный запуск ничего не дублирует: компоненты переиспользуются, UI пересоздаётся.
///
/// Из командной строки: -executeMethod VictorySetup.SetupGameSceneBatch (открывает, настраивает
/// и сохраняет GameScene).
/// </summary>
public static class VictorySetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string ScreenName = "VictoryScreen";
    private const string TimerName = "MatchTimerLabel";
    private const string MoneyName = "MoneyLabel";

    private static readonly Color Red = new Color(0.91f, 0.14f, 0.24f);

    [MenuItem("Bloody Steak/Setup Victory & Economy")]
    private static void SetupMenu() => Setup();

    public static void SetupGameSceneBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (!Setup()) EditorApplication.Exit(1);
        EditorSceneManager.SaveScene(scene);
    }

    private static bool Setup()
    {
        var orderManager = Object.FindFirstObjectByType<OrderManager>();
        var gameOver = Object.FindFirstObjectByType<GameOverManager>();
        var difficulty = Object.FindFirstObjectByType<DifficultyManager>();
        var canvas = FindByName<Canvas>("GameUICanvas");
        if (orderManager == null || gameOver == null || canvas == null)
        {
            const string message = "Не найдены OrderManager, GameOverManager или GameUICanvas. " +
                                   "Откройте GameScene, выполните Setup Game Over и запустите снова.";
            if (Application.isBatchMode) Debug.LogError(message);
            else EditorUtility.DisplayDialog("Victory Setup", message, "OK");
            return false;
        }

        var host = GameObject.Find("Managers") ?? orderManager.gameObject;

        var timer = GetOrAdd<MatchTimer>(host);
        Assign(gameOver, "matchTimer", timer);
        if (difficulty != null) Assign(difficulty, "matchTimer", timer);

        var money = GetOrAdd<MoneyManager>(host);
        Assign(money, "orderManager", orderManager);
        Assign(money, "difficulty", difficulty);

        var score = GetOrAdd<ScoreManager>(host);
        Assign(score, "orderManager", orderManager);
        Assign(score, "gameOver", gameOver);
        Assign(score, "money", money);

        SetupHud(canvas, timer, money);
        SetupScreen(canvas, gameOver, score);

        EditorSceneManager.MarkSceneDirty(host.scene);
        Debug.Log("Victory & Economy: настройка завершена. Сохраните сцену (Ctrl+S).");
        return true;
    }

    private static void SetupHud(Canvas canvas, MatchTimer timer, MoneyManager money)
    {
        // Тёмная плашка под текстом: белый текст иначе теряется на светлом кафеле кухни.
        var timerText = CreateHudLabel(canvas.transform, TimerName, "5:00",
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(240, 96));
        var timerUI = timerText.transform.parent.gameObject.AddComponent<MatchTimerUI>();
        Assign(timerUI, "timer", timer);
        Assign(timerUI, "label", timerText);

        var moneyText = CreateHudLabel(canvas.transform, MoneyName, "$ 0",
            new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(320, 96));
        var moneyUI = moneyText.transform.parent.gameObject.AddComponent<MoneyUI>();
        Assign(moneyUI, "money", money);
        Assign(moneyUI, "label", moneyText);
    }

    private static TextMeshProUGUI CreateHudLabel(Transform canvas, string name, string text,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        DestroyChild(canvas, name);
        var plate = CreateUIObject(name, canvas);
        plate.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        SetRect((RectTransform)plate.transform, anchor, anchor, position, size);

        var label = CreateText("Label", plate.transform, text, 60);
        Stretch(label.gameObject);
        return label;
    }

    private static void SetupScreen(Canvas canvas, GameOverManager manager, ScoreManager score)
    {
        DestroyChild(canvas.transform, ScreenName);

        // Хост с VictoryUI всегда активен, а root (сам экран) выключается до победы.
        var host = CreateUIObject(ScreenName, canvas.transform);
        Stretch(host);
        host.transform.SetAsLastSibling(); // поверх остального HUD

        var root = CreateUIObject("Root", host.transform);
        Stretch(root);

        var dim = CreateUIObject("DimOverlay", root.transform);
        Stretch(dim);
        var dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0f);

        var panel = CreateUIObject("Panel", root.transform);
        Stretch(panel);
        var panelGroup = panel.AddComponent<CanvasGroup>();

        var title = CreateText("Title", panel.transform, "VICTORY", 120);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 200));

        var breakdown = CreateText("Breakdown", panel.transform, "", 44);
        breakdown.color = new Color(0.8f, 0.8f, 0.8f);
        SetRect(breakdown.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 140));

        var total = CreateText("Total", panel.transform, "", 72);
        total.color = Red;
        SetRect(total.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 110));

        var restart = CreateButton("RestartButton", panel.transform, "Начать заново");
        SetRect((RectTransform)restart.transform, new Vector2(0.5f, 0.27f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 90));

        var menu = CreateButton("MainMenuButton", panel.transform, "Выход в меню");
        SetRect((RectTransform)menu.transform, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 90));

        var ui = host.AddComponent<VictoryUI>();
        Assign(ui, "manager", manager);
        Assign(ui, "score", score);
        Assign(ui, "root", root);
        Assign(ui, "dimOverlay", dimImage);
        Assign(ui, "panel", panelGroup);
        Assign(ui, "titleLabel", title);
        Assign(ui, "breakdownLabel", breakdown);
        Assign(ui, "totalLabel", total);
        Assign(ui, "restartButton", restart);
        Assign(ui, "mainMenuButton", menu);

        root.SetActive(false);
    }

    // ---- helpers ----

    private static void Assign(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

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

    private static void DestroyChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
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

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size)
    {
        var go = CreateUIObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
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
