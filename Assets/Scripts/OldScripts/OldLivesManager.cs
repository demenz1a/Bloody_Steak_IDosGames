using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OldLivesManager : MonoBehaviour
{
    public static OldLivesManager Instance;

    [Header("Lives")]
    public int maxLives = 5;
    public int lives;

    [Header("UI")]
    public Image[] lifeIcons; 

    [Header("Game Over")]
    public string gameOverSceneName = "BadEnding";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        lives = maxLives;
        RefreshUI();
    }

    public void LoseLife()
    {
        lives = Mathf.Max(0, lives - 1);
        RefreshUI();

        if (lives <= 0)
            SceneManager.LoadScene(gameOverSceneName);
    }

    public void ResetLives()
    {
        lives = maxLives;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
                lifeIcons[i].enabled = (i < lives);
        }
    }
}
