/// <summary>Состояния жизненного цикла клиента (см. диаграмму в п. 9.1).</summary>
public enum NpcState
{
    MovingToOrderPoint,
    WaitingForOrderCreation,
    WaitingForFood,
    MovingToTable,
    Eating,
    MovingToToilet,
    AtToilet,
    MovingToExit,
    Panicking,
    Dead
}
