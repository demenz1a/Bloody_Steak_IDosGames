using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Переключает активную Cinemachine-камеру в зависимости от формы игрока.
/// Сглаживание перехода (зум, позиция, скорость блендинга) настраивается
/// в CinemachineBrain и в самих vcam — этот скрипт только меняет Priority.
/// </summary>
public class CameraFormSwitcher : MonoBehaviour
{
    [SerializeField] private PlayerFormController playerForm;
    [SerializeField] private CinemachineCamera kitchenCamera;
    [SerializeField] private CinemachineCamera hallCamera;

    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;

    private void OnEnable()
    {
        playerForm.OnFormChanged += HandleFormChanged;
        // Выставляем актуальное состояние сразу при старте сцены,
        // а не только при первой смене формы.
        HandleFormChanged(playerForm.CurrentForm);
    }

    private void OnDisable()
    {
        playerForm.OnFormChanged -= HandleFormChanged;
    }

    private void HandleFormChanged(PlayerForm newForm)
    {
        bool isWolf = newForm == PlayerForm.Wolf;

        kitchenCamera.Priority = isWolf ? inactivePriority : activePriority;
        hallCamera.Priority = isWolf ? activePriority : inactivePriority;
    }
}