using UnityEngine;

public class ActivateObject : MonoBehaviour
{
    public GameObject targetObject;
    public Animator animator1;
    public Animator animator2;
    public KeyCode key = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            animator1.SetTrigger("New Trigger");
            animator2.SetTrigger("New Trigger");
        }
    }
}