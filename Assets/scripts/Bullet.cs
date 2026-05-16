using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;

    private void OnTriggerEnter(Collider collision)
    {
        Target target = collision.GetComponent<Target>();
        if (target != null)
        {
            target.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}