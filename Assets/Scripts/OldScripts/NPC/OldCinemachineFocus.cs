using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class OldCinemachineFocus : MonoBehaviour
{
    [Header("Refs")]
    public CinemachineBrain brain;                 // Brain на активной Main Camera
    public CinemachineCamera inspectorVcam; // vcam инспектора

    private ICinemachineCamera prevCam;
    private int prevInspectorPriority;

    public void FocusInspector(float seconds)
    {
        StartCoroutine(FocusRoutine(seconds));
    }

    private IEnumerator FocusRoutine(float seconds)
    {
        if (brain == null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        // запомнили текущую активную виртуальную камеру
        prevCam = brain.ActiveVirtualCamera;

        // подняли приоритет инспектору
        prevInspectorPriority = inspectorVcam.Priority;
        inspectorVcam.Priority = 999;

        yield return new WaitForSeconds(seconds);

        // вернули приоритет обратно
        inspectorVcam.Priority = prevInspectorPriority;

        // НИЧЕГО больше делать не надо: Cinemachine сам вернётся к тем камерам,
        // которые были активны по своим правилам.
    }
}
