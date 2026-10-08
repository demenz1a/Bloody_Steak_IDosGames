using UnityEngine;
using UnityEngine.UI;

public class OldBlackoutCooldownUI : MonoBehaviour
{
    public Image fillImage;

    void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();
    }

    void Update()
    {
        if (OldBlackoutManager.Instance == null) return;

        fillImage.fillAmount = OldBlackoutManager.Instance.CooldownProgress;

        fillImage.enabled = fillImage.fillAmount > 0f;
    }
}
