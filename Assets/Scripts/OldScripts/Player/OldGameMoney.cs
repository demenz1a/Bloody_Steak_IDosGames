using UnityEngine;
using TMPro;

public class OldGameMoney : MonoBehaviour
{
    public static OldGameMoney Instance;

    public int money;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;

    void Awake()
    {
        Instance = this;
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateMoneyUI();
    }

    public void SpendMoney(int amount)
    {
        if (amount > money)
        {
            Debug.Log("Not enough money!");
            return;
        }

        money -= amount;
        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = money.ToString();
    }
}
