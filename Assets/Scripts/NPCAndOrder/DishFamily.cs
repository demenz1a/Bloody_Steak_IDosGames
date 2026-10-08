/// <summary>
/// Категория блюда, которую хочет клиент. Заказ хранит именно это, а не конкретный
/// ProductType — клиенту всё равно, Steak1 у него в тарелке или Steak2 (слегка/сильно
/// прожаренный), важно только что это "стейк". Сопоставление ProductType -> DishFamily
/// см. в ProductFamily.cs.
/// </summary>
public enum DishFamily
{
    Steak,
    Sausage,
    Skewer
}
