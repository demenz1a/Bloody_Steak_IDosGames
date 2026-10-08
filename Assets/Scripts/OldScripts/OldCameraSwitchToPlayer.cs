using Unity.Cinemachine;
using UnityEngine;

public class OldCameraSwitchToPlayer : MonoBehaviour
{
    public bool switchToPlayer;
    public bool switchToKitchen;
    public bool changeSortingOrder;
    public CinemachineCamera KitchenCamera;
    public CinemachineCamera PlayerCamera;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer ClueSpriteRenderer;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (switchToKitchen)
        {
            KitchenCamera.Priority = 1;
            PlayerCamera.Priority = 0;
            spriteRenderer.sortingOrder = 5;
        }

        else if (switchToPlayer)
        {
            KitchenCamera.Priority = 0;
            PlayerCamera.Priority = 1;
        }

        else if (changeSortingOrder)
        {
            spriteRenderer.sortingOrder = 4;
            ClueSpriteRenderer.enabled = false;
        }

    }
}
