using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Настройка рубильника в открытой GameScene: меню Bloody Steak/Setup Light Switch.
/// Объект рубильника (LightSwtich / LightSwitch) получает всё, что есть у станций, по образцу гриля:
/// слой Interactable, материал с обводкой, коллайдер того же типа, InteractionHighlightView и
/// копию радиального таймера над собой (перезарядка). В GameUICanvas добавляется затемнение
/// BlackoutOverlay (первым ребёнком — под HUD). Звук щелчка назначается из Assets/Audio/LightSwitch.
/// Повторный запуск ничего не дублирует. Batch: -executeMethod LightSwitchSetup.SetupBatch.
/// </summary>
public static class LightSwitchSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string OverlayName = "BlackoutOverlay";
    private const string TimerName = "CooldownTimer";

    [MenuItem("Bloody Steak/Setup Light Switch")]
    private static void SetupMenu() => Setup();

    public static void SetupBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Setup();
        EditorSceneManager.SaveScene(scene);
    }

    private static void Setup()
    {
        var sw = FindByName("LightSwtich") ?? FindByName("LightSwitch");
        var station = Object.FindObjectsByType<CookingStation>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(s => s.name == "Grill") ?? Object.FindFirstObjectByType<CookingStation>();
        if (sw == null || station == null)
        {
            Debug.LogError("Light Switch: не найден объект LightSwtich или станция-образец (Grill). Откройте GameScene.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(sw, "Setup Light Switch");

        // 1. Слой и материал с обводкой — как у станции.
        sw.layer = station.gameObject.layer;
        var swRenderer = sw.GetComponent<SpriteRenderer>();
        var stationRenderer = station.GetComponent<SpriteRenderer>() ?? station.GetComponentInChildren<SpriteRenderer>();
        if (swRenderer != null && stationRenderer != null) swRenderer.sharedMaterial = stationRenderer.sharedMaterial;

        // 2. Коллайдер того же типа (по размеру спрайта рубильника).
        var stationCollider = station.GetComponent<Collider2D>();
        if (sw.GetComponent<Collider2D>() == null)
        {
            var type = stationCollider != null ? stationCollider.GetType() : typeof(BoxCollider2D);
            var collider = (Collider2D)Undo.AddComponent(sw, type);
            if (stationCollider != null) collider.isTrigger = stationCollider.isTrigger;
        }

        // 3. Обводка с теми же цветом и толщиной.
        var highlight = sw.GetComponent<InteractionHighlightView>() ?? Undo.AddComponent<InteractionHighlightView>(sw);
        var stationHighlight = station.GetComponent<InteractionHighlightView>();
        if (stationHighlight != null)
        {
            var from = new SerializedObject(stationHighlight);
            var to = new SerializedObject(highlight);
            to.FindProperty("highlightColor").colorValue = from.FindProperty("highlightColor").colorValue;
            to.FindProperty("outlineWidth").floatValue = from.FindProperty("outlineWidth").floatValue;
            to.ApplyModifiedProperties();
        }

        // 4. Таймер перезарядки — копия таймера станции, над рубильником.
        var timer = SetupTimer(sw, station, swRenderer, stationRenderer);

        // 5. Логика.
        var lightSwitch = sw.GetComponent<LightSwitch>() ?? Undo.AddComponent<LightSwitch>(sw);
        var so = new SerializedObject(lightSwitch);
        so.FindProperty("cooldownUI").objectReferenceValue = timer;
        so.ApplyModifiedProperties();

        SetupOverlay();
        SoundSetup.AssignClipsFromFolders();

        EditorSceneManager.MarkSceneDirty(sw.scene);
        Debug.Log("Light Switch: настройка завершена. Сохраните сцену (Ctrl+S).");
    }

    private static CookingTimerUI SetupTimer(GameObject sw, CookingStation station, SpriteRenderer swRenderer, SpriteRenderer stationRenderer)
    {
        var existing = sw.transform.Find(TimerName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        var stationTimer = new SerializedObject(station).FindProperty("timerUI").objectReferenceValue as CookingTimerUI;
        if (stationTimer == null)
        {
            Debug.LogWarning("Light Switch: у станции-образца нет таймера — назначьте cooldownUI вручную.");
            return null;
        }

        // Копируем ветку таймера целиком (от прямого ребёнка станции), сохраняя мировой масштаб.
        var branch = stationTimer.transform;
        while (branch.parent != null && branch.parent != station.transform) branch = branch.parent;

        var copy = Object.Instantiate(branch.gameObject, sw.transform, worldPositionStays: true);
        copy.name = TimerName;
        Undo.RegisterCreatedObjectUndo(copy, "Create cooldown timer");

        // Та же высота над верхним краем спрайта, что у станции.
        if (swRenderer != null && stationRenderer != null)
        {
            float above = branch.position.y - stationRenderer.bounds.max.y;
            var b = swRenderer.bounds;
            copy.transform.position = new Vector3(b.center.x, b.max.y + above, branch.position.z);
        }

        copy.SetActive(true);
        var timer = copy.GetComponentInChildren<CookingTimerUI>(true);
        return timer;
    }

    private static void SetupOverlay()
    {
        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "GameUICanvas");
        if (canvas == null)
        {
            Debug.LogWarning("Light Switch: не найден GameUICanvas — затемнение не создано.");
            return;
        }

        var existing = canvas.transform.Find(OverlayName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        var go = new GameObject(OverlayName, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(go, "Create blackout overlay");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling(); // темнеет мир, HUD остаётся поверх

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        var ui = go.AddComponent<BlackoutOverlayUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("overlay").objectReferenceValue = image;
        so.ApplyModifiedProperties();
    }

    private static GameObject FindByName(string objectName) =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == objectName)
            .Select(t => t.gameObject)
            .FirstOrDefault();
}
