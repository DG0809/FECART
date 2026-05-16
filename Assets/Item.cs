using UnityEngine;

public class Item : MonoBehaviour
{
    private Rigidbody rb;
    private bool isCollected = false;
    private PlayerController playerController;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = FindObjectOfType<PlayerController>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            playerController.SetLastItemCollider(GetComponent<Collider>());
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerController.RemoveLastItemCollider(GetComponent<Collider>());
        }
    }

    public void Collect(Transform handPoint)
    {
        if (isCollected) return;

        isCollected = true;
        rb.isKinematic = true;

        transform.parent = handPoint;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        GetComponent<Collider>().enabled = false;
    }
}