using UnityEngine;

public class OldKey : MonoBehaviour
{
    public SpriteRenderer keySpriteRenderer;
    public Collider2D wallCollider;
    private bool isDoorClosed;

    public void Interact()
    {
        isDoorClosed = !isDoorClosed;
        keySpriteRenderer.enabled = !isDoorClosed;
        wallCollider.enabled = !isDoorClosed;

    }
}
