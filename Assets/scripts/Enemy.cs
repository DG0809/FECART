using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Visão")]
    [SerializeField] private float viewRadius = 15f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Movimento")]
    [SerializeField] private float wanderRange = 30f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float waitTimeAtDestination = 2f;

    [Header("Perseguição")]
    [SerializeField] private float losePlayerDistance = 20f;
    [SerializeField] private float waitTimeAfterLose = 3f;

    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    private float waitTimer = 0f;
    private bool isChasing = false;
    private bool isInvestigating = false;
    private float investigateTimer = 0f;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        Player player = FindObjectOfType<Player>();

        if (player != null)
        {
            playerTransform = player.transform;
        }

        StartWandering();
    }

    private void Update()
    {
        CheckPlayerVisibility();
        UpdateBehavior();
    }

    private void CheckPlayerVisibility()
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer =
            (playerTransform.position - transform.position).normalized;

        float distanceToPlayer =
            Vector3.Distance(transform.position, playerTransform.position);

        // Fora do raio
        if (distanceToPlayer > viewRadius)
        {
            LosePlayer();
            return;
        }

        // Fora do ângulo
        float angleToPlayer =
            Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > viewAngle / 2)
        {
            LosePlayer();
            return;
        }

        // Obstáculo bloqueando visão
        if (Physics.Raycast(
            transform.position + Vector3.up * 0.6f,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayer))
        {
            LosePlayer();
            return;
        }

        // Player visto
        isChasing = true;
        isInvestigating = false;
        lastKnownPlayerPosition = playerTransform.position;
    }

    private void LosePlayer()
    {
        if (isChasing)
        {
            isChasing = false;
            isInvestigating = true;
            investigateTimer = waitTimeAfterLose;
        }
    }

    private void UpdateBehavior()
    {
        if (isChasing)
        {
            ChasePlayer();
            return;
        }

        if (isInvestigating)
        {
            InvestigateLastPosition();
            return;
        }

        Wander();
    }

    private void ChasePlayer()
    {
        navMeshAgent.SetDestination(playerTransform.position);
    }

    private void InvestigateLastPosition()
    {
        investigateTimer -= Time.deltaTime;

        if (investigateTimer <= 0)
        {
            isInvestigating = false;
            StartWandering();
            return;
        }

        navMeshAgent.SetDestination(lastKnownPlayerPosition);
    }

    private void Wander()
    {
        if (!navMeshAgent.hasPath ||
            navMeshAgent.remainingDistance < stoppingDistance)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                Vector3 randomDestination =
                    GetRandomNavMeshPosition(transform.position, wanderRange);

                navMeshAgent.SetDestination(randomDestination);

                waitTimer = waitTimeAtDestination;
            }
        }
    }

    private void StartWandering()
    {
        Vector3 randomDestination =
            GetRandomNavMeshPosition(transform.position, wanderRange);

        navMeshAgent.SetDestination(randomDestination);

        waitTimer = waitTimeAtDestination;
    }

    private Vector3 GetRandomNavMeshPosition(Vector3 center, float range)
    {
        Vector3 randomDirection =
            Random.insideUnitSphere * range;

        randomDirection += center;

        NavMeshHit hit;

        Vector3 finalPosition = center;

        if (NavMesh.SamplePosition(
            randomDirection,
            out hit,
            range,
            NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
    }

    // GIZMOS
    private void OnDrawGizmosSelected()
    {
        // Raio de visão
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Linhas laterais
        Vector3 leftBoundary =
            Quaternion.Euler(0, -viewAngle / 2, 0) *
            transform.forward *
            viewRadius;

        Vector3 rightBoundary =
            Quaternion.Euler(0, viewAngle / 2, 0) *
            transform.forward *
            viewRadius;

        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            transform.position,
            transform.position + leftBoundary);

        Gizmos.DrawLine(
            transform.position,
            transform.position + rightBoundary);

        // Linha até player
        if (isChasing && playerTransform != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                transform.position,
                playerTransform.position);
        }

        // Investigando
        if (isInvestigating)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(
                transform.position,
                lastKnownPlayerPosition);
        }
    }

    // Quando o player atira
    public void OnPlayerShot(Vector3 shotPosition)
    {
        if (!isChasing)
        {
            isInvestigating = true;
            lastKnownPlayerPosition = shotPosition;
            investigateTimer = waitTimeAfterLose;
        }
    }

    public bool IsChasing()
    {
        return isChasing;
    }
}