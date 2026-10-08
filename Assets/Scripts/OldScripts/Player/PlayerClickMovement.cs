using UnityEngine;
using UnityEngine.AI;

public class PlayerClickMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = -Camera.main.transform.position.z;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            agent.SetDestination(mouseWorldPos);
        }
    }
}
