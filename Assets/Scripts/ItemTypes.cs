/// <summary>Категория предмета, который игрок может нести в руках.</summary>
public enum CarriableItemType
{
    RawMeat,
    CookedDish,
    CorpseBag
}

/// <summary>Конкретный тип готового блюда — имеет значение только когда ItemType == CookedDish.</summary>
public enum DishType
{
    Steak,   // Гриль
    Skewer,  // Шампуры
    Sausage  // Мясорубка
}