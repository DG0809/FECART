using UnityEngine;
using static UnityEditor.Progress;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform handPoint;

    private Rigidbody rb;
    private Collider lastItemCollider;
    private Vector3 moveDirection = Vector3.zero;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Criar HandPoint se n�o existir
        if (handPoint == null)
        {
            GameObject hand = new GameObject("HandPoint");
            hand.transform.SetParent(transform);
            hand.transform.localPosition = new Vector3(0, 1, 0.5f);
            handPoint = hand.transform;
        }
    }

    private void Update()
    {
        HandleInput();
        HandleInteract();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void HandleInput()
    {
        moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A))
            moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            moveDirection += Vector3.right;

        moveDirection = moveDirection.normalized;
    }

    private void Move()
    {
        if (moveDirection.magnitude > 0)
        {
            rb.linearVelocity = new Vector3(
                moveDirection.x * moveSpeed,
                rb.linearVelocity.y,
                moveDirection.z * moveSpeed
            );

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void HandleInteract()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (lastItemCollider != null)
            {
                Item item = lastItemCollider.GetComponent<Item>();
                if (item != null)
                {
                    item.Collect(handPoint);
                }
            }
        }
    }

    public void SetLastItemCollider(Collider collider)
    {
        lastItemCollider = collider;
    }

    public void RemoveLastItemCollider(Collider collider)
    {
        if (lastItemCollider == collider)
        {
            lastItemCollider = null;
        }
    }
}