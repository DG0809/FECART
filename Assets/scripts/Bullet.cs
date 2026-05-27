using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    private bool hasHit = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (hasHit) return;

        // Destruir ao bater em obstáculos
        if (collision.CompareTag("Obstacle") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        // Dar dano ao inimigo
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            hasHit = true;
            enemy.TakeDamage(damageAmount);
            Destroy(gameObject);
            return;
        }

        // Destruir ao bater em qualquer outra coisa (exceto player)
        if (!collision.CompareTag("Player"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        if (!collision.gameObject.CompareTag("Player"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}