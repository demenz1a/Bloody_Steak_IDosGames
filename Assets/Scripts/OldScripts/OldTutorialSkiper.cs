using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OldTutorialSkiper : MonoBehaviour
{
    [SerializeField] private GameObject blackOutForScreenTransition;
    public void TurnOff()
    {
        gameObject.SetActive(false);
    }

    public void NextScene()
    {
        Instantiate(blackOutForScreenTransition);
        StartCoroutine(StartScene() );
    }

    IEnumerator StartScene()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("SampleScene");
    }
}
