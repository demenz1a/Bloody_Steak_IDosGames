using UnityEngine;

public class OldUpgrades : MonoBehaviour
{
    public OldCookStation cookStation1;
    public OldCookStation cookStation2;
    public OldCookStation cookStation3;
    public OldCookStation cookStation4;
    public GameObject KeyButton;
    public Collider2D KeyWallCollider;
    public GameObject LightOutButton;

    public void UpgradeLightOut()
    {
        if (OldGameMoney.Instance.money < 100)
            return;

        OldGameMoney.Instance.SpendMoney(100);
        //LightOutButton.SetActive(true);
        OldBlackoutManager.Instance.cooldown -= 10f;
        gameObject.SetActive(false);
    }

    public void UpgradeKey()
    {
        if (OldGameMoney.Instance.money < 100)
            return;

        OldGameMoney.Instance.SpendMoney(100);
        KeyButton.SetActive(true);
        KeyWallCollider.enabled = true;
        gameObject.SetActive(false);
    }

    public void UpgradeFittings()
    {
        if (OldGameMoney.Instance.money < 100)
            return;

            OldGameMoney.Instance.SpendMoney(100);
            cookStation1.burnTime += 2f;
            cookStation1.cookTime -= 1.5f;
            cookStation2.burnTime += 2f;
            cookStation2.cookTime -= 1.5f;
            cookStation3.burnTime += 2f;
            cookStation3.cookTime -= 1.5f;
            cookStation4.burnTime += 2f;
            cookStation4.cookTime -= 1.5f;
            gameObject.SetActive(false);
    }

    public void UpgradeSilence()
    {
        if (OldGameMoney.Instance.money < 100)
            return;

        OldGameMoney.Instance.SpendMoney(100);
        OldPlayerMovement.Instance.speed += 2f;
        gameObject.SetActive(false);
    }
}
