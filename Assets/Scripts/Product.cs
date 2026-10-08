using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Физический объект мяса/блюда. Один и тот же экземпляр живёт от "сырое мясо"
/// до "готовое блюдо" (или "сгорело") — станция вызывает SetType(), объект сам
/// обновляет свой спрайт. Не содержит Collider2D/Rigidbody2D — перемещается только
/// через HoldPoint.Place(), физика в этом не участвует.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Product : CarriableItem
{
    [System.Serializable]
    private struct Visual
    {
        public ProductType type;
        public Sprite sprite;
    }

    [Tooltip("Спрайт для каждой возможной стадии. Один префаб мяса должен покрывать ВСЕ стадии, " +
             "т.к. заранее не известно, на какую станцию оно попадёт.")]
    [SerializeField] private Visual[] visuals;

    private SpriteRenderer _spriteRenderer;
    private Dictionary<ProductType, Sprite> _visualLookup;

    public ProductType Type { get; private set; } = ProductType.RawMeat;
    public bool IsBurned => Type == ProductType.Burned;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _visualLookup = new Dictionary<ProductType, Sprite>();
        foreach (var v in visuals)
        {
            _visualLookup[v.type] = v.sprite;
        }

        UpdateSprite();
    }

    public void SetType(ProductType newType)
    {
        Type = newType;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_visualLookup != null && _visualLookup.TryGetValue(Type, out var sprite))
        {
            if (Type == ProductType.Burned)
            {
                _spriteRenderer.color = Color.black;
            }
            else
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}
