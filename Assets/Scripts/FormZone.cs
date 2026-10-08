using UnityEngine;

/// <summary>
/// Триггер-зона, которая переключает форму игрока при входе в неё.
///
/// Расстановка в сцене:
/// - На кухне повесить один экземпляр с formOnEnter = Sheep.
/// - В зале повесить один экземпляр с formOnEnter = Wolf.
/// - На буферную зону (кладовку между кухней и залом) НЕ вешать этот компонент вообще —
///   именно отсутствие триггера там и даёт требуемое поведение "в буфере трансформации нет".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FormZone : MonoBehaviour
{
    [SerializeField] private PlayerForm formOnEnter;

    private void Reset()
    {
        // Подстраховка: коллайдер зоны обязан быть триггером,
        // иначе он начнёт мешать физическому перемещению игрока.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var formController = other.GetComponent<PlayerFormController>();
        if (formController == null) return;

        if (formOnEnter == PlayerForm.Sheep)
        {
            var corpseState = other.GetComponent<PlayerCorpseState>();
            if (corpseState != null && corpseState.IsCarrying) return; // с трупом на кухню нельзя
        }

        formController.SetForm(formOnEnter);
    }
}
