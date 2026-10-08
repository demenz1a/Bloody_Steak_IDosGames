using System.Collections.Generic;
using UnityEngine;

public class OldNPCSpawner : MonoBehaviour
{
    public static OldNPCSpawner Instance;

    [Header("Spawn")]
    public GameObject npcPrefab;
    public GameObject telkaPrefab;
    public Transform spawnPoint;
    public float spawnDelay = 4f;

    [Header("Difficulty")]
    public float minSpawnDelay = 1f;          // ниже этого не опускаем
    public int decreaseEverySpawns = 3;       // каждые 3 спавна
    public float decreaseBySeconds = 1f;      // на сколько уменьшать

    [Header("Limits")]
    public int maxNPC = 12;
    public OldOrderTrigger[] orderPoints;

    private List<OldNPCAI> activeNPCs = new List<OldNPCAI>();
    private float spawnTimer;

    private int totalSpawned = 0;
    private bool telkaSpawned = false;

    private float currentSpawnDelay;

    void Awake()
    {
        Instance = this;
        currentSpawnDelay = spawnDelay;
        spawnTimer = currentSpawnDelay; 
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            bool spawned = TrySpawn();                 
            spawnTimer = currentSpawnDelay;           
        }
    }

    bool TrySpawn()
    {
        if (activeNPCs.Count >= maxNPC) return false;
        if (!HasFreeOrderPoint()) return false;
        if (OldPlayerMovement.Instance.isKiller) return false;

        bool spawnTelkaNow = !telkaSpawned && (totalSpawned == 6);

        GameObject prefabToSpawn = spawnTelkaNow ? telkaPrefab : npcPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogError("PrefabToSpawn is NULL (npcPrefab/telkaPrefab).");
            return false;
        }

        GameObject npcGO = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

        OldNPCAI npc = npcGO.GetComponent<OldNPCAI>();

        if (npc == null)
        {
            Debug.LogError("На префабе нет компонента OldNPCAI (или наследника).");
            Destroy(npcGO);
            return false;
        }

        npc.exit = spawnPoint.position; 

        var p = GetOrderPointFor(npc);
        if (p == null)
        {
            Destroy(npcGO);
            return false;
        }

        npc.orderPoint = p.transform;
        npc.assignedOrderPoint = p;

        activeNPCs.Add(npc);
        npc.OnDestroyed += RemoveNPC;

        totalSpawned++;

        if (spawnTelkaNow)
            telkaSpawned = true;

        if (totalSpawned % decreaseEverySpawns == 0)
        {
            currentSpawnDelay = Mathf.Max(minSpawnDelay, currentSpawnDelay - decreaseBySeconds);
            // Debug.Log($"[Spawner] Difficulty up! spawnDelay = {currentSpawnDelay}");
        }

        return true;
    }

    void RemoveNPC(OldNPCAI npc)
    {
        activeNPCs.Remove(npc);
    }

    bool HasFreeOrderPoint()
    {
        foreach (var point in orderPoints)
            if (!point.isOccupied) return true;
        return false;
    }

    public OldOrderTrigger GetOrderPointFor(OldNPCAI npc)
    {
        foreach (var point in orderPoints)
            if (point.TryOccupy(npc))
                return point;
        return null;
    }
}
