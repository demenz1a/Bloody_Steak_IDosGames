using UnityEngine;

public class OldTrashBin : MonoBehaviour
{
    public void Interact(OldPlayerPickUp player)
    {
        if (player.heldProduct == null) return;

        Destroy(player.heldProduct.gameObject);
        player.heldProduct = null;
    }
}
