using UnityEngine;

public class OldProduct : MonoBehaviour
{
    public OldProductType productType;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    public Sprite rawMeatSprite;
    public Sprite sausageSprite;
    public Sprite sashlikSprite;

    public Sprite steak1Sprite;
    public Sprite steak2Sprite;
    public Sprite steak3Sprite;

    public Sprite burnedSprite;

    [Header("Cooking")]
    public bool isCooking;
    public bool isReady;
    public bool isBurned;

    void Awake()
    {
        UpdateSprite();
    }

    public void SetType(OldProductType newType)
    {
        productType = newType;
        UpdateSprite();
    }

    void UpdateSprite()
    {
        switch (productType)
        {
            case OldProductType.RawMeat:
                spriteRenderer.sprite = rawMeatSprite;
                break;

            case OldProductType.Sausage:
                spriteRenderer.sprite = sausageSprite;
                break;

            case OldProductType.Sashlik:
                spriteRenderer.sprite = sashlikSprite;
                break;

            case OldProductType.Steak1:
                spriteRenderer.sprite = steak1Sprite;
                break;

            case OldProductType.Steak2:
                spriteRenderer.sprite = steak2Sprite;
                break;

            case OldProductType.Steak3:
                spriteRenderer.sprite = steak3Sprite;
                break;

            case OldProductType.BurnedMeat:
                spriteRenderer.color = Color.black;
                break;
        }
    }
}
