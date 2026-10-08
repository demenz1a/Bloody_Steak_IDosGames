using UnityEngine;
using System;

public class OldKillCounter : MonoBehaviour
{
    public static OldKillCounter Instance;

    public static event Action<int> OnKillCountChanged; 

    public int KillCount { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    public void RegisterKill()
    {
        KillCount++;
        OnKillCountChanged?.Invoke(KillCount);
        Debug.Log($"[OldKillCounter] KillCount = {KillCount}");
    }

    public void ResetKills()
    {
        KillCount = 0;
        OnKillCountChanged?.Invoke(KillCount);
    }
}
