using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class OnClickScriptsForButtons : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private GameObject blackOutForScreenTransition;
    [SerializeField] private PauseManager PauseManager;
    [SerializeField] private GameObject Something;
    private bool IsSomethingOpened;

    public void StartNextScene()
    {
        SceneManager.LoadScene(sceneName);
    }
    public void instantiateBlackOutForScreenTransition()
    {
        Instantiate(blackOutForScreenTransition);
        StartCoroutine(StartScene());
    }


    public void OpenAndClose() 
    {
        IsSomethingOpened = !IsSomethingOpened;
        Something.SetActive(!IsSomethingOpened); 
    }
    IEnumerator StartScene()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnInGame()
    {
        PauseManager.TurnOffPause();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
