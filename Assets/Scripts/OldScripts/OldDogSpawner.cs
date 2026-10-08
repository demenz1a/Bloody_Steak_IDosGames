using UnityEngine;

public class OldDogSpawner : MonoBehaviour
{
    [Header("Dog")]
    public OldDogAI dog;

    void OnEnable()
    {
        OldKillCounter.OnKillCountChanged += HandleKills;
    }

    void OnDisable()
    {
        OldKillCounter.OnKillCountChanged -= HandleKills;
    }

    void HandleKills(int kills)
    {
        if (dog == null) return;
        if (dog.gameObject.activeSelf) return;

        bool shouldSpawn =
            (kills == 1) || (kills >= 3 && kills % 2 == 1);

        if (!shouldSpawn) return;

        dog.Show();
    }
}
