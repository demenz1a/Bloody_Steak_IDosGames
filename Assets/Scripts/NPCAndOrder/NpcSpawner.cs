using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Периодически пытается создать нового клиента. Спаун гейтится доступностью
/// точки заказа (п. 9.5: "если все 3 поинта заняты — заказчики не появляются"),
/// а НЕ доступностью слота Order — эти две вещи разделены сознательно (см. OrderManager).
/// </summary>
public class NpcSpawner : MonoBehaviour
{
    [SerializeField] private NpcCustomer npcPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;

    [SerializeField] private PointGroup orderPoints;
    [SerializeField] private PointGroup tablePoints;
    [SerializeField] private PointGroup toiletPoints;
    [SerializeField] private OrderManager orderManager;

    [Header("Spawn timing")]
    [SerializeField] private float minSpawnInterval = 4f;
    [SerializeField] private float maxSpawnInterval = 9f;

    [Header("Order size")]
    [Range(0f, 1f)]
    [SerializeField] private float twoItemOrderChance = 0.35f;

    private float _spawnTimer;

    private void Update()
    {
        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            TrySpawn();
            _spawnTimer = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    private void TrySpawn()
    {
        if (!orderPoints.TryReserveFirstFree(out var reservedOrderPoint)) return;

        var npc = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        var dishes = RandomDishes();

        npc.Initialize(orderManager, tablePoints, toiletPoints, exitPoint, reservedOrderPoint, dishes);
    }

    private List<DishFamily> RandomDishes()
    {
        var values = (DishFamily[])Enum.GetValues(typeof(DishFamily));
        int count = UnityEngine.Random.value < twoItemOrderChance ? 2 : 1;

        var result = new List<DishFamily>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(values[UnityEngine.Random.Range(0, values.Length)]);
        }
        return result;
    }
}
