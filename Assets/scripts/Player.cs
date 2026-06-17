using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Referências")]
    [SerializeField] private Transform handPoint;
    [SerializeField] private Camera mainCamera;

    [Header("Interação")]
    [SerializeField] private float interactDistance = 3f;

    private Rigidbody rb;
    private Collider lastItemCollider;
    private Vector3 moveDirection;
    private Transform cameraTransform;
    private float rotationX;
    private Weapon equippedWeapon;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

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
        }

        cameraTransform = mainCamera.transform;

        if (cameraTransform.parent != transform)
        {
            cameraTransform.SetParent(transform);
            cameraTransform.localPosition = new Vector3(0, 0.6f, 0);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused())
            return;

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
        if (rb == null) return;

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

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    private void HandleInteract()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (TryInteractByRaycast())
            return;

        if (lastItemCollider == null) return;

        Weapon weapon = lastItemCollider.GetComponent<Weapon>();

        if (weapon != null)
        {
            EquipWeapon(weapon);
        }
    }

    private bool TryInteractByRaycast()
    {
        if (mainCamera == null) return false;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            return false;

        WeaponShelf shelf = hit.collider.GetComponentInParent<WeaponShelf>();

        if (shelf != null)
        {
            shelf.OpenShelf();
            return true;
        }

        Weapon weapon = hit.collider.GetComponentInParent<Weapon>();

        if (weapon != null)
        {
            EquipWeapon(weapon);
            return true;
        }

        return false;
    }

    private void EquipWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;
        weapon.Collect(handPoint, this);
    }

    public void SetLastItemCollider(Collider collider)
    {
        lastItemCollider = collider;
    }

    public void RemoveLastItemCollider(Collider collider)
    {
        if (lastItemCollider == collider)
            lastItemCollider = null;
    }

    public bool HasEquippedWeapon()
    {
        return equippedWeapon != null;
    }

    public Camera GetPlayerCamera()
    {
        return mainCamera;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameStateManager.Instance.GoToDefeat();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            GameStateManager.Instance.GoToDefeat();
        }
    }
}