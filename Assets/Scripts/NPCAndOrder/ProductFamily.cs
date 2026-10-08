/// <summary>
/// Переводит конкретную стадию продукта (Steak1/Steak2/Sausage/Skewer) в общую
/// категорию блюда, которую сравнивает система заказов. RawMeat и Burned
/// не относятся ни к одной категории — их нельзя отдать ни на один заказ.
/// </summary>
public static class ProductFamily
{
    public static bool TryGetFamily(ProductType type, out DishFamily family)
    {
        switch (type)
        {
            case ProductType.Steak1:
            case ProductType.Steak2:
                family = DishFamily.Steak;
                return true;

            case ProductType.Sausage:
                family = DishFamily.Sausage;
                return true;

            case ProductType.Skewer:
                family = DishFamily.Skewer;
                return true;

            default: // RawMeat, Burned
                family = default;
                return false;
        }
    }
}
