using System;

public static class OldKillerLockdown
{
    public static bool IsLockdown { get; private set; }

    // событие: кто-то попытался убить во время запрета
    public static event Action OnMurderDuringLockdown;

    public static void Begin()
    {
        IsLockdown = true;
    }

    public static void End()
    {
        IsLockdown = false;
    }

    // вызывать, когда игрок пытается убить в локдауне
    public static void ReportAttempt()
    {
        OnMurderDuringLockdown?.Invoke();
    }
}
