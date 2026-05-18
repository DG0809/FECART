using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private int health = 30;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Alvo atingido! Vida restante: {health}");

        if (health <= 0)
        {
            Destroy(gameObject);
            Debug.Log("Alvo destruído!");
        }
    }
}