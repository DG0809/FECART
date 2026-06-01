using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private enum EnemyState { Patrol, Suspicion, Chase }
    private EnemyState currentState = EnemyState.Patrol;

    [Header("Movimento")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float wanderRange = 25f;
    [SerializeField] private float patrolWaitTime = 3f;

    [Header("Visão")]
    [SerializeField] private float viewRadius = 12f;
    [SerializeField] private float viewAngle = 110f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Comportamento")]
    [SerializeField] private float suspicionDuration = 5f;
    [SerializeField] private float chaseLoseTime = 4f;

    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPos;
    private float stateTimer = 0f;
    private float chaseTimer = 0f;
    private int health = 30;
    private bool isDead = false;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        playerTransform = FindObjectOfType<Player>().transform;

        if (navMeshAgent == null)
        {
            Debug.LogError("Enemy precisa de NavMeshAgent!");
            return;
        }

        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.angularSpeed = 60f;
        navMeshAgent.stoppingDistance = 0.5f;
        navMeshAgent.avoidancePriority = 50;
        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

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
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
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
        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
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
        if (playerTransform != null)
        {
            navMeshAgent.SetDestination(playerTransform.position);
        }

        chaseTimer -= Time.deltaTime;
        if (chaseTimer <= 0)
        {
            currentState = EnemyState.Suspicion;
            lastKnownPlayerPos = playerTransform.position;
            navMeshAgent.SetDestination(lastKnownPlayerPos);
            navMeshAgent.speed = patrolSpeed;
            stateTimer = suspicionDuration;
        }
    }

    private void CheckPlayerVisibility()
    {
        if (playerTransform == null || currentState == EnemyState.Chase) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > viewRadius) return;

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > viewAngle / 2) return;

        if (Physics.Raycast(transform.position + Vector3.up * 0.6f, directionToPlayer, distanceToPlayer, obstacleLayer))
            return;

        EnterChase();
    }

    private void EnterChase()
    {
        if (currentState == EnemyState.Chase) return;

        currentState = EnemyState.Chase;
        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.angularSpeed = 120f;
        chaseTimer = chaseLoseTime;
        lastKnownPlayerPos = playerTransform.position;

        Debug.Log("Inimigo em PERSEGUIÇÃO!");
    }

    private void StartPatrol()
    {
        currentState = EnemyState.Patrol;
        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.angularSpeed = 60f;
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
        if (currentState == EnemyState.Chase) return;

        currentState = EnemyState.Suspicion;
        lastKnownPlayerPos = shotPosition;
        navMeshAgent.SetDestination(lastKnownPlayerPos);
        navMeshAgent.speed = patrolSpeed;
        stateTimer = suspicionDuration;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        navMeshAgent.enabled = false;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        if (currentState == EnemyState.Chase)
        {
            Gizmos.color = Color.red;
            if (playerTransform != null)
                Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}