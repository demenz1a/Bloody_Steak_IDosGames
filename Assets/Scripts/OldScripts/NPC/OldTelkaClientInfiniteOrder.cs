using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class OldTelkaClientInfiniteOrder : MonoBehaviour
{
    private OldTelkaAI telkaAI;

    [Header("Order")]
    public OldProductType[] currentOrder = new OldProductType[2];
    private bool[] delivered = new bool[2];

    [Header("UI Order (slots)")]
    public SpriteRenderer slot1;
    public SpriteRenderer slot2;
    public Sprite[] productSprites;
    public GameObject bubble;

    [Header("Complete message")]
    public TextMeshProUGUI messageText;
    [TextArea] public string[] successPhrases;
    public float typeSpeed = 0.03f;
    public float showTimeAfter = 0.8f;

    [Header("Choice UI")]
    public GameObject choiceCanvas;     // Canvas (SetActive false � ������)
    public Button yesButton;            // ������ "��"
    public Button noButton;             // ������ "���"
    public string endingSceneName = "EndingScene"; // ��� ����� ��������

    [Header("Reward (optional)")]
    public int reward = 100; // ���� ����� � ����� ������� 0

    private bool finished; // ����� �� ��������� ������

    private void Awake()
    {
        telkaAI = GetComponentInParent<OldTelkaAI>();

        if (choiceCanvas) choiceCanvas.SetActive(false);
        if (messageText) messageText.gameObject.SetActive(false);

        // ����������� ������ (����� �� ����������� OnClick �������)
        if (yesButton) yesButton.onClick.AddListener(OnYes);
        if (noButton) noButton.onClick.AddListener(OnNo);
    }

    public void bubbleshow()
    {
        if (bubble != null)
            bubble.SetActive(true);
    }

    private void Start()
    {
        bubbleshow();
        GenerateOrder();
        UpdateUI();
    }

    void GenerateOrder()
    {
        currentOrder[0] = (OldProductType)Random.Range(1, 6);
        currentOrder[1] = (OldProductType)Random.Range(1, 6);

        delivered[0] = false;
        delivered[1] = false;
    }

    void UpdateUI()
    {
        if (slot1) slot1.sprite = delivered[0] ? null : productSprites[(int)currentOrder[0] - 1];
        if (slot2) slot2.sprite = delivered[1] ? null : productSprites[(int)currentOrder[1] - 1];
    }

    public void Interact(OldPlayerPickUp player)
    {
        if (finished) return;
        if (player == null || player.heldProduct == null) return;

        for (int i = 0; i < currentOrder.Length; i++)
        {
            if (delivered[i]) continue;

            if (currentOrder[i] == player.heldProduct.productType)
            {
                delivered[i] = true;

                Destroy(player.heldProduct.gameObject);
                player.heldProduct = null;

                UpdateUI();

                if (OrderCompleted())
                {
                    StartCoroutine(CompleteRoutine());
                }

                return;
            }
        }
    }

    bool OrderCompleted()
    {
        return delivered[0] && delivered[1];
    }

    IEnumerator CompleteRoutine()
    {
        finished = true;

        // ������� (���� �����)
        if (reward > 0 && OldGameMoney.Instance != null)
            OldGameMoney.Instance.AddMoney(reward);

        // �������� ������ �����
        if (slot1) slot1.enabled = false;
        if (slot2) slot2.enabled = false;

        // ������ �����
        if (messageText)
        {
            messageText.gameObject.SetActive(true);

            string phrase = (successPhrases != null && successPhrases.Length > 0)
                ? successPhrases[Random.Range(0, successPhrases.Length)]
                : "This place sucks... If you come with me, I'll show you something more interesting";

            yield return StartCoroutine(TypeText(phrase));
            yield return new WaitForSeconds(showTimeAfter);
        }

        // ���������� �����
        if (choiceCanvas)
        {
            choiceCanvas.SetActive(true);
            Time.timeScale = 0f; // ����� ����
        }
    }


    IEnumerator TypeText(string text)
    {
        messageText.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            messageText.text += text[i];
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    void OnNo()
    {
        if (choiceCanvas) choiceCanvas.SetActive(false);

        if (telkaAI != null)
            telkaAI.GoOutNow();

        Time.timeScale = 1f;

        //gameObject.SetActive(false);
    }


    void OnYes()
    {
        // "��" -> ������ ����� ��������
        // (���� ����� �������� ������ � ����� ����� static / DontDestroyOnLoad)
        SceneManager.LoadScene(endingSceneName);
        Time.timeScale = 1f;
    }
}