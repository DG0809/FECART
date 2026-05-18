using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform handPoint;
    [SerializeField] private Camera mainCamera;

    private Rigidbody rb;
    private Collider lastItemCollider;
    private Vector3 moveDirection = Vector3.zero;
    private Transform cameraTransform;
    private float rotationX = 0f;
    private Weapon equippedWeapon;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (handPoint == null)
        {
            GameObject hand = new GameObject("HandPoint");
            hand.transform.SetParent(transform);
            hand.transform.localPosition = new Vector3(0, 0.6f, 0.3f);
            handPoint = hand.transform;
        }

        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("MainCamera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0);
            mainCamera = cameraObj.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            cameraTransform = cameraObj.transform;
        }
        else
        {
            cameraTransform = mainCamera.transform;
            if (cameraTransform.parent != transform)
            {
                cameraTransform.SetParent(transform);
                cameraTransform.localPosition = new Vector3(0, 0.6f, 0);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleInput();
        HandleMouse();
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
            moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += transform.right;

        moveDirection = moveDirection.normalized;
        moveDirection.y = 0;
    }

    private void Move()
    {
        Vector3 velocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );

        rb.linearVelocity = velocity;
    }

    private void HandleMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotação horizontal do player
        Vector3 playerRotation = transform.eulerAngles;
        playerRotation.y += mouseX;
        transform.eulerAngles = playerRotation;

        // Rotação vertical só da camera
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
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

                    Weapon weapon = lastItemCollider.GetComponent<Weapon>();
                    if (weapon != null)
                    {
                        equippedWeapon = weapon;
                        weapon.NotifyCollected();
                    }
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

    public bool HasEquippedWeapon()
    {
        return equippedWeapon != null;
    }
}