using System.Collections.Generic;
using UnityEngine;

public class OldMeatTable : MonoBehaviour
{
    public static OldMeatTable Instance;
    [Header("Meat settings")]
    public GameObject rawMeatPrefab;
    public List<Transform> meatSlots = new List<Transform>();

    private List<OldProduct> meatsOnTable = new List<OldProduct>();

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        SpawnAllMeat();
    }

    private void Update()
    {

    }

    void TakeMeat(OldPlayerPickUp player)
    {
        OldProduct meat = meatsOnTable[0];
        meatsOnTable.RemoveAt(0);

        player.TakeProduct(meat);
    }

    void SpawnAllMeat()
    {
        foreach (Transform slot in meatSlots)
        {
            GameObject meat = Instantiate(rawMeatPrefab, slot.position, Quaternion.identity, slot);
            OldProduct product = meat.GetComponent<OldProduct>();
            meatsOnTable.Add(product);
        }
    }

    public void Refill()
    {
        //if (meatsOnTable.Count > 0) return;

        SpawnAllMeat();
    }

    public void Interact(OldPlayerPickUp player)
    {
        if (player.heldProduct != null) return;
        if (meatsOnTable.Count == 0) return;

        OldProduct meat = meatsOnTable[0];
        meatsOnTable.RemoveAt(0);
        player.TakeProduct(meat);
    }

}