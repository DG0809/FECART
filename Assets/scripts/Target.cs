using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private int health = 30;

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