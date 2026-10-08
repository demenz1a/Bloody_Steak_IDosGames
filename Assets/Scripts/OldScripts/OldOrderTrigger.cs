using UnityEngine;

public class OldOrderTrigger : MonoBehaviour
{
    public bool isOccupied;

    private OldNPCAI occupant;

    public bool TryOccupy(OldNPCAI customer)
    {
        if (isOccupied || customer == null) return false;

        isOccupied = true;
        occupant = customer;
        return true;
    }

    public void ForceRelease(OldNPCAI customer)
    {
        if (!isOccupied) return;
        if (occupant != customer) return;

        occupant = null;
        isOccupied = false;
    }
}

