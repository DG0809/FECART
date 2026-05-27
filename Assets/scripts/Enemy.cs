using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private enum EnemyState { Patrol, Suspicion, Chase }

    [Header("Estados")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("Movimento - Patrulha")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolAngularSpeed = 60f;
    [SerializeField] private float patrolWaitTime = 3f;
    [SerializeField] private float wanderRange = 25f;

    [Header("Movimento - Perseguição")]
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float chaseAngularSpeed = 120f;

    [Header("Visão")]
    [SerializeField] private float viewRadius = 12f;
    [SerializeField] private float viewAngle = 110f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Comportamento")]
    [SerializeField] private float suspicionDuration = 5f;
    [SerializeField] private float chaseLoseTime = 4f;

    [Header("Vida")]
    [SerializeField] private int health = 30;

    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPos;
    private float stateTimer = 0f;
    private float chaseTimer = 0f;
    private bool isDead = false;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.angularSpeed = patrolAngularSpeed;
        navMeshAgent.stoppingDistance = 0.5f;

        StartPatrol();
    }

    private void Update()
    {
        if (isDead) return;

        CheckPlayerVisibility();
        UpdateState();
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;

            case EnemyState.Suspicion:
                UpdateSuspicion();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.5f)
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0)
            {
                Vector3 randomPos = GetRandomNavMeshPosition(transform.position, wanderRange);
                navMeshAgent.SetDestination(randomPos);
                stateTimer = patrolWaitTime;
            }
        }
    }

    private void UpdateSuspicion()
    {
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.5f)
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0)
            {
                StartPatrol();
            }
        }
    }

    private void UpdateChase()
    {
        if (playerTransform == null) return;

        navMeshAgent.SetDestination(playerTransform.position);

        if (CanSeePlayer())
        {
            chaseTimer = chaseLoseTime;
            lastKnownPlayerPos = playerTransform.position;
        }
        else
        {
            chaseTimer -= Time.deltaTime;

            if (chaseTimer <= 0)
            {
                EnterSuspicion(lastKnownPlayerPos);
            }
        }
    }

    private void CheckPlayerVisibility()
    {
        if (playerTransform == null) return;
        if (currentState == EnemyState.Chase) return;

        if (CanSeePlayer())
        {
            EnterChase();
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * 0.6f;
        Vector3 targetPosition = playerTransform.position + Vector3.up * 0.6f;

        Vector3 directionToPlayer = (targetPosition - eyePosition).normalized;
        float distanceToPlayer = Vector3.Distance(eyePosition, targetPosition);

        if (distanceToPlayer > viewRadius) return false;

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > viewAngle / 2f) return false;

        if (Physics.Raycast(eyePosition, directionToPlayer, distanceToPlayer, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    private void EnterChase()
    {
        currentState = EnemyState.Chase;

        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.angularSpeed = chaseAngularSpeed;

        chaseTimer = chaseLoseTime;
        lastKnownPlayerPos = playerTransform.position;
    }

    private void EnterSuspicion(Vector3 position)
    {
        currentState = EnemyState.Suspicion;

        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.angularSpeed = patrolAngularSpeed;

        lastKnownPlayerPos = position;
        navMeshAgent.SetDestination(lastKnownPlayerPos);

        stateTimer = suspicionDuration;
    }

    private void StartPatrol()
    {
        currentState = EnemyState.Patrol;

        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.angularSpeed = patrolAngularSpeed;

        Vector3 randomPos = GetRandomNavMeshPosition(transform.position, wanderRange);
        navMeshAgent.SetDestination(randomPos);

        stateTimer = patrolWaitTime;
    }

    private Vector3 GetRandomNavMeshPosition(Vector3 center, float range)
    {
        Vector3 randomDirection = Random.insideUnitSphere * range;
        randomDirection += center;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return center;
    }

    public void OnPlayerShot(Vector3 shotPosition)
    {
        if (isDead) return;
        if (currentState == EnemyState.Chase) return;

        EnterSuspicion(shotPosition);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftBoundary =
            Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward * viewRadius;

        Vector3 rightBoundary =
            Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward * viewRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        if (currentState == EnemyState.Chase && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
        else if (currentState == EnemyState.Suspicion)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lastKnownPlayerPos);
            Gizmos.DrawWireSphere(lastKnownPlayerPos, 0.3f);
        }
    }

    public bool IsChasing()
    {
        return currentState == EnemyState.Chase;
    }
}