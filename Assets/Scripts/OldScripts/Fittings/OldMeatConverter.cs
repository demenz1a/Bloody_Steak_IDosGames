using UnityEngine;

public class OldMeatConverter : MonoBehaviour
{
    public void Interact(OldPlayerKiller player)
    {
        OldMeatTable.Instance.Refill();
    }
}
