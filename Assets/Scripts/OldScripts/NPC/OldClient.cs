using TMPro;
using UnityEngine;
using System.Collections;

public class OldClient : MonoBehaviour
{
    private OldNPCAI npcai;

    [Header("Timer UI")]
    public UnityEngine.UI.Image timerImage;

    public Color moodGreen = Color.green;
    public Color moodYellow = Color.yellow;
    public Color moodRed = Color.red;

    [Header("Order")]
    public OldProductType[] currentOrder = new OldProductType[2];
    private bool[] delivered = new bool[2];

    [Header("Mood & Time")]
    public float maxTime = 1f;
    private float currentTime;
    public int mood = 3;

    [Header("Reward")]
    public int baseReward = 100;

    [Header("UI")]
    public SpriteRenderer slot1;
    public SpriteRenderer slot2;
    public Sprite[] productSprites;

    [Header("Complete Message")]
    public TextMeshProUGUI messageText;
    [TextArea] public string[] successPhrases;
    public float typeSpeed = 0.03f;
    public float showTimeAfter = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successClip;

    private void Awake()
    {
        npcai = GetComponentInParent<OldNPCAI>();
    }

    void Start()
    {
        GenerateOrder();
        currentTime = maxTime;
    }

    void Update()
    {
        currentTime -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTime <= 0)
            LoseMood();
    }

    void UpdateTimerUI()
    {
        timerImage.fillAmount = currentTime / maxTime;

        switch (mood)
        {
            case 3: timerImage.color = moodGreen; break;
            case 2: timerImage.color = moodYellow; break;
            case 1: timerImage.color = moodRed; break;
        }
    }

    void GenerateOrder()
    {
        OldProductType first = (OldProductType)Random.Range(1, 6);
        OldProductType second;

        do
        {
            second = (OldProductType)Random.Range(1, 6);
        }
        while (!IsValidPair(first, second));

        currentOrder[0] = first;
        currentOrder[1] = second;

        delivered[0] = false;
        delivered[1] = false;

        UpdateUI();
    }

    bool IsValidPair(OldProductType a, OldProductType b)
    {
        if (a == b) return false;

        bool aSteak = IsSteak(a);
        bool bSteak = IsSteak(b);
        if (aSteak && bSteak) return false;

        return true;
    }

    bool IsSteak(OldProductType t)
    {
        return t == OldProductType.Steak1 ||
               t == OldProductType.Steak2 ||
               t == OldProductType.Steak3;
    }

    void UpdateUI()
    {
        slot1.sprite = delivered[0] ? null : productSprites[(int)currentOrder[0] - 1];
        slot2.sprite = delivered[1] ? null : productSprites[(int)currentOrder[1] - 1];
    }

    void LoseMood()
    {
        mood--;
        mood = Mathf.Clamp(mood, 0, 3);
        currentTime = maxTime;

        if (mood <= 0)
            FailOrder();
    }

    public void Interact(OldPlayerPickUp player)
    {
        if (player.heldProduct == null) return;

        for (int i = 0; i < currentOrder.Length; i++)
        {
            if (delivered[i]) continue;

            if (currentOrder[i] == player.heldProduct.productType)
            {
                delivered[i] = true;

                Destroy(player.heldProduct.gameObject);
                player.heldProduct = null;

                UpdateUI();
                PlaySuccess();

                if (OrderCompleted())
                    CompleteOrder();

                return;
            }
        }
    }

    bool OrderCompleted()
    {
        return delivered[0] && delivered[1];
    }

    void CompleteOrder()
    {
        int reward = baseReward * mood;
        OldGameMoney.Instance.AddMoney(reward);

        StartCoroutine(ShowSuccessAndLeave());
    }

    IEnumerator ShowSuccessAndLeave()
    {
        if (messageText == null || successPhrases.Length == 0)
        {
            Leave();
            yield break;
        }

        string phrase = successPhrases[Random.Range(0, successPhrases.Length)];

        messageText.gameObject.SetActive(true);
        messageText.text = "";

        // ������ �� ������
        foreach (char c in phrase)
        {
            messageText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // ��������� ����� ������
        yield return new WaitForSeconds(showTimeAfter);

        messageText.gameObject.SetActive(false);

        Leave();
    }


    void FailOrder()
    {
        npcai.MarkOrderFailed();
        OldLivesManager.Instance?.LoseLife();
        gameObject.SetActive(false);
    }

    void PlaySuccess()
    {
        if (audioSource && successClip)
            audioSource.PlayOneShot(successClip);
    }

    void Leave()
    {
        npcai.MarkOrderCompleted();
        gameObject.SetActive(false);
    }

}