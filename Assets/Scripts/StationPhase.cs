/// <summary>Текущая фаза станции. Пересчитывается каждый кадр из прошедшего времени, не хранится "напрямую".</summary>
public enum StationPhase
{
    Empty,
    Cooking,
    ReadyToServe,
    Burnt
}
