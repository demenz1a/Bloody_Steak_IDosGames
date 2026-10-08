using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlackOut : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadSceneBlackOut()
    {
        SceneManager.LoadScene(sceneName);
    }
}
