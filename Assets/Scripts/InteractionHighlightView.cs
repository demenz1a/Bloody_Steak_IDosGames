using UnityEngine;

/// <summary>
/// Обводка объекта взаимодействия. Работает "из коробки" на любом наборе спрайтов —
/// если персонаж составной (несколько частей тела, каждая свой SpriteRenderer, как у NPC),
/// обводка применяется сразу ко ВСЕМ дочерним рендерерам одновременно, одним включением.
///
/// Требует, чтобы на каждом из этих SpriteRenderer был назначен материал с шейдером
/// Custom/SpriteOutline. Управление идёт через MaterialPropertyBlock — не создаёт
/// отдельный экземпляр материала на каждый рендерер, поэтому дёшево.
///
/// Публичный API (SetActive(bool), SetColor(Color)) не изменился — PlayerInteractor
/// и NpcCustomer трогать не нужно.
/// </summary>
public class InteractionHighlightView : MonoBehaviour
{
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private float outlineWidth = 1.5f;

    [Tooltip("Если оставить пустым — автоматически берутся ВСЕ SpriteRenderer в дочерних объектах " +
             "(подходит и для одного простого спрайта, и для составного персонажа из многих частей).")]
    [SerializeField] private SpriteRenderer[] renderers;

    private MaterialPropertyBlock _propertyBlock;
    private bool _isActive;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        _propertyBlock = new MaterialPropertyBlock();
        SetActive(false);
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        ApplyToAllRenderers();
    }

    /// <summary>
    /// Сменить цвет обводки на лету — нужно объектам с несколькими возможными действиями
    /// (например, NPC: жёлтая обводка для выдачи заказа, красная — для убийства).
    /// Если обводка сейчас активна, применяется немедленно, а не только со следующего SetActive(true).
    /// </summary>
    public void SetColor(Color color)
    {
        highlightColor = color;
        if (_isActive) ApplyToAllRenderers();
    }

    private void ApplyToAllRenderers()
    {
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorId, highlightColor);
            _propertyBlock.SetFloat(OutlineWidthId, outlineWidth);
            _propertyBlock.SetFloat(OutlineEnabledId, _isActive ? 1f : 0f);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
