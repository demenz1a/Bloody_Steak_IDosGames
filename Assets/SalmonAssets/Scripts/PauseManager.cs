using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseManager;
    private bool isPaused;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isPaused == false)
        {
            pauseManager.SetActive(true);
            isPaused = true;
            Time.timeScale = 0.0f;
        }

        else if (Input.GetKeyDown(KeyCode.Escape) && isPaused)
        {
            TurnOffPause();
        }
    }

    public void TurnOffPause()
    {
        pauseManager.SetActive(false);
        isPaused = false;
        Time.timeScale = 1.0f;
    }
}
