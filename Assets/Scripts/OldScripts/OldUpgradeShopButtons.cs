using UnityEngine;

public class OldUpgradeShopButtons : MonoBehaviour
{
    public bool isUpgradeShopOpened = false;
    public GameObject upgradeShopUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleUpgradeShop();
        }
    }

    public void ToggleUpgradeShop()
    {
        isUpgradeShopOpened = !isUpgradeShopOpened;
        upgradeShopUI.SetActive(isUpgradeShopOpened);
    }

    public void OpenUpgradeShop()
    {
        //Time.timeScale = 0f;
        isUpgradeShopOpened = true;
        upgradeShopUI.SetActive(true);
    }

    public void CloseUpgradeShop()
    {
        //Time.timeScale = 1f;
        isUpgradeShopOpened = false;
        upgradeShopUI.SetActive(false);
    }

    public void OpenAndCloseUpgradeShop() { isUpgradeShopOpened = !isUpgradeShopOpened; upgradeShopUI.SetActive(!isUpgradeShopOpened); }
}
