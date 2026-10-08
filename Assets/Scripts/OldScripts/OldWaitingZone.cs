using TMPro;
using UnityEngine;
using System.Collections;

public class OldWaitingZone : MonoBehaviour
{

    public TextMeshProUGUI timerText;
    //private Coroutine timerCoroutine;
    public float time;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out OldNPCAI npcai))
            StartCoroutine(BubbleTimer(time));
    }

    IEnumerator BubbleTimer(float time)
    {
        int secondsLeft = Mathf.CeilToInt(time);

        timerText.gameObject.SetActive(true);

        while (secondsLeft > 0)
        {
            timerText.text = secondsLeft.ToString();
            yield return new WaitForSeconds(1f);
            secondsLeft--;
        }

        timerText.gameObject.SetActive(false);
    }
}
