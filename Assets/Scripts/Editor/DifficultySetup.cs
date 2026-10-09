using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Одноразовая настройка системы сложности в открытой GameScene: меню Bloody Steak/Setup Difficulty.
/// Добавляет DifficultyManager на Managers и назначает его в OrderManager и NpcSpawner.
/// Повторный запуск ничего не дублирует.
/// </summary>
public static class DifficultySetup
{
    [MenuItem("Bloody Steak/Setup Difficulty")]
    private static void Setup()
    {
        var orderManager = Object.FindFirstObjectByType<OrderManager>();
        var spawner = Object.FindFirstObjectByType<NpcSpawner>();
        if (orderManager == null || spawner == null)
        {
            EditorUtility.DisplayDialog("Difficulty Setup",
                "Не найдены OrderManager или NpcSpawner. Откройте GameScene и запустите снова.", "OK");
            return;
        }

        var host = GameObject.Find("Managers") ?? orderManager.gameObject;
        var difficulty = host.GetComponent<DifficultyManager>();
        if (difficulty == null) difficulty = Undo.AddComponent<DifficultyManager>(host);

        Assign(orderManager, difficulty);
        Assign(spawner, difficulty);

        EditorSceneManager.MarkSceneDirty(host.scene);
        Debug.Log("Difficulty: настройка завершена. Сохраните сцену (Ctrl+S).");
    }

    private static void Assign(Object target, DifficultyManager difficulty)
    {
        var so = new SerializedObject(target);
        so.FindProperty("difficulty").objectReferenceValue = difficulty;
        so.ApplyModifiedProperties();
    }
}
