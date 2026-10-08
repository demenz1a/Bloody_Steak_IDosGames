using UnityEngine;

public class OldColaMachine : MonoBehaviour
{
    public GameObject colaPrefab;
    public Transform spawnPoint;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;

        OldPlayerPickUp player = other.GetComponent<OldPlayerPickUp>();
        if (player == null || player.heldProduct != null) return;

        GameObject cola = Instantiate(colaPrefab, spawnPoint.position, Quaternion.identity);
        player.TakeProduct(cola.GetComponent<OldProduct>());
    }
}
