using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador de inimigo com IA baseada em estados para ambiente interno (casa).
///
/// Funcionalidades:
///   - Patrulha por waypoints manuais ou pontos aleatórios validados
///   - Investigação com DoorPoints para transitar entre cômodos
///   - Perseguição via NavMeshAgent
///   - Validação de destino: rejeita pontos próximos de paredes
///   - LookAround que verifica direções livres antes de girar
///   - Raycasts frontais, laterais e diagonais para evitar paredes
///   - Gizmos completos para debug visual no Editor
///
/// Componentes obrigatórios no GameObject:
///   - NavMeshAgent
///   - CapsuleCollider
///   - Rigidbody (IsKinematic = true, UseGravity = false) — opcional
///
/// Tags e Layers necessárias:
///   - Tag "Player" no objeto do jogador
///   - Layer "Obstacle" nas paredes (campo wallLayer)
///   - Layer "Player" no jogador (campo playerLayer)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    // ENUM DE ESTADOS
    // ═══════════════════════════════════════════════

    public enum EnemyState
    {
        Patrol,
        Investigate,
        Chase
    }

    // ═══════════════════════════════════════════════
    // INSPECTOR — VISÃO
    // ═══════════════════════════════════════════════

    [Header("Visão")]
    [Tooltip("Distância máxima de detecção do Player.")]
    [SerializeField] private float viewRadius = 12f;

    [Tooltip("Ângulo total do cone de visão em graus.")]
    [SerializeField] private float viewAngle = 110f;

    [Tooltip("Layer do objeto Player.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Layer de paredes e obstáculos que bloqueiam a visão.")]
    [SerializeField] private LayerMask obstacleLayer;

    // ═══════════════════════════════════════════════
    // INSPECTOR — EVITAR PAREDES
    // ═══════════════════════════════════════════════

    [Header("Evitar Paredes")]
    [Tooltip("Layer das paredes usada nos raycasts de proximidade.")]
    [SerializeField] private LayerMask wallLayer;

    [Tooltip("Distância de detecção de parede nos raycasts laterais e frontal.")]
    [SerializeField] private float wallCheckDistance = 1.2f;

    [Tooltip("Raio mínimo seguro entre o destino escolhido e qualquer parede. " +
             "Destinos mais próximos do que este valor são rejeitados.")]
    [SerializeField] private float wallSafeDistance = 0.8f;

    [Tooltip("Raio da esfera usada em Physics.CheckSphere para validar destinos.")]
    [SerializeField] private float destinationCheckRadius = 0.6f;

    // ═══════════════════════════════════════════════
    // INSPECTOR — PATRULHA
    // ═══════════════════════════════════════════════

    [Header("Patrulha — Waypoints")]
    [Tooltip("Lista de pontos de patrulha manuais. Se vazia, usa pontos aleatórios.")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Tooltip("Se verdadeiro, percorre os patrolPoints em ordem. " +
             "Se falso, escolhe aleatoriamente.")]
    [SerializeField] private bool patrolInOrder = true;

    [Header("Patrulha — Movimento")]
    [Tooltip("Velocidade durante a patrulha.")]
    [SerializeField] private float patrolSpeed = 2.2f;

    [Tooltip("Raio de busca para pontos aleatórios (usado quando patrolPoints está vazio).")]
    [SerializeField] private float wanderRadius = 18f;

    [Tooltip("Tempo mínimo de espera ao chegar em um waypoint.")]
    [SerializeField] private float minWaitAtWaypoint = 2f;

    [Tooltip("Tempo máximo de espera ao chegar em um waypoint.")]
    [SerializeField] private float maxWaitAtWaypoint = 5f;

    [Tooltip("Velocidade angular ao olhar para os lados (graus/segundo).")]
    [SerializeField] private float lookAroundSpeed = 55f;

    [Tooltip("Ângulo máximo de rotação ao olhar para os lados (cada direção).")]
    [SerializeField] private float maxLookAngle = 70f;

    // ═══════════════════════════════════════════════
    // INSPECTOR — PORTAS (DOORPOINTS)
    // ═══════════════════════════════════════════════

    [Header("Portas — DoorPoints")]
    [Tooltip("Lista de Transforms posicionados no centro de cada porta da casa. " +
             "O inimigo usa o DoorPoint mais próximo ao destino ao trocar de cômodo.")]
    public List<Transform> doorPoints = new List<Transform>();

    [Tooltip("Se verdadeiro, o inimigo tenta passar por DoorPoints ao investigar " +
             "um destino em outro cômodo.")]
    [SerializeField] private bool useDoorPoints = true;

    [Tooltip("Distância máxima para considerar que um DoorPoint é relevante para o trajeto.")]
    [SerializeField] private float doorPointRelevanceDistance = 8f;

    // ═══════════════════════════════════════════════
    // INSPECTOR — PERSEGUIÇÃO
    // ═══════════════════════════════════════════════

    [Header("Perseguição")]
    [SerializeField] private float chaseSpeed = 5.2f;

    [Tooltip("Distância em que o inimigo para de avançar (ataque/confronto).")]
    [SerializeField] private float attackRange = 1.4f;

    // ═══════════════════════════════════════════════
    // INSPECTOR — INVESTIGAÇÃO
    // ═══════════════════════════════════════════════

    [Header("Investigação")]
    [SerializeField] private float investigateSpeed = 3.8f;

    [Tooltip("Tempo total de investigação antes de voltar à patrulha.")]
    [SerializeField] private float investigateDuration = 8f;

    [Tooltip("Raio de incerteza adicionado à posição do disparo.")]
    [SerializeField] private float shotNoiseRadius = 2.5f;

    // ═══════════════════════════════════════════════
    // REFERÊNCIAS PRIVADAS
    // ═══════════════════════════════════════════════

    private NavMeshAgent agent;
    private Transform playerTransform;
    private Vector3 spawnPosition;

    // ═══════════════════════════════════════════════
    // ESTADO INTERNO
    // ═══════════════════════════════════════════════

    private EnemyState currentState = EnemyState.Patrol;

    // Patrol
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private Coroutine patrolWaitCoroutine;

    // Investigate
    private Vector3 lastKnownPosition;
    private float investigateTimer;

    // LookAround
    private bool isLookingAround = false;
    private Coroutine lookAroundCoroutine;

    // Cache para Gizmos de wall-check (atualizado no Update)
    private bool wallFront, wallLeft, wallRight, wallDiagFL, wallDiagFR;

    // ═══════════════════════════════════════════════
    // UNITY — INICIALIZAÇÃO
    // ═══════════════════════════════════════════════

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("[Enemy] Player não encontrado. Verifique a tag 'Player'.");

        // Impede que o NavMeshAgent cause flutuação ou rotação indesejada.
        agent.updateRotation = true;
        agent.updateUpAxis = false;

        EnterPatrol();
    }

    private void Update()
    {
        UpdateWallSensors();
        CheckPlayerVisibility();
        UpdateStateMachine();
    }

    // ═══════════════════════════════════════════════
    // SENSORES DE PAREDE
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Executa raycasts em cinco direções para detectar paredes próximas.
    /// Os resultados são usados tanto em tempo real (para ajustar rotação)
    /// quanto nos Gizmos para visualização no Editor.
    /// </summary>
    private void UpdateWallSensors()
    {
        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 fwd = transform.forward;

        wallFront = Physics.Raycast(pos, fwd, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(pos, Quaternion.Euler(0f, -90f, 0f) * fwd, wallCheckDistance * 0.7f, wallLayer);
        wallRight = Physics.Raycast(pos, Quaternion.Euler(0f, 90f, 0f) * fwd, wallCheckDistance * 0.7f, wallLayer);
        wallDiagFL = Physics.Raycast(pos, Quaternion.Euler(0f, -45f, 0f) * fwd, wallCheckDistance * 0.9f, wallLayer);
        wallDiagFR = Physics.Raycast(pos, Quaternion.Euler(0f, 45f, 0f) * fwd, wallCheckDistance * 0.9f, wallLayer);
    }

    /// <summary>
    /// Verifica se uma direção específica está livre de paredes.
    /// Usado pelo LookAround para não girar em direção a uma parede.
    /// </summary>
    private bool IsDirectionClear(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        return !Physics.Raycast(origin, direction.normalized, wallCheckDistance * 1.3f, wallLayer);
    }

    /// <summary>
    /// Verifica se um ponto de destino está suficientemente longe de paredes.
    /// Rejeita pontos colados em obstáculos.
    /// </summary>
    private bool IsDestinationSafe(Vector3 point)
    {
        // CheckSphere detecta qualquer collider de parede dentro do raio seguro
        if (Physics.CheckSphere(point, destinationCheckRadius, wallLayer))
            return false;

        // Confirma com raycasts nas 4 direções cardeais a partir do ponto
        Vector3 checkOrigin = point + Vector3.up * 0.5f;
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(checkOrigin, dir, wallSafeDistance, wallLayer))
                return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════
    // DETECÇÃO DO PLAYER
    // ═══════════════════════════════════════════════

    private void CheckPlayerVisibility()
    {
        if (playerTransform == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        Vector3 toPlayer = playerTransform.position - origin;
        float distance = toPlayer.magnitude;
        Vector3 toPlayerNorm = toPlayer.normalized;

        if (distance > viewRadius) { OnPlayerLost(); return; }

        float angle = Vector3.Angle(transform.forward, toPlayerNorm);
        if (angle > viewAngle * 0.5f) { OnPlayerLost(); return; }

        if (Physics.Raycast(origin, toPlayerNorm, distance, obstacleLayer))
        {
            OnPlayerLost();
            return;
        }

        OnPlayerSpotted();
    }

    private void OnPlayerSpotted()
    {
        lastKnownPosition = playerTransform.position;
        if (currentState != EnemyState.Chase)
            EnterChase();
    }

    private void OnPlayerLost()
    {
        if (currentState == EnemyState.Chase)
            EnterInvestigate(lastKnownPosition);
    }

    // ═══════════════════════════════════════════════
    // MÁQUINA DE ESTADOS
    // ═══════════════════════════════════════════════

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Chase: UpdateChase(); break;
        }
    }

    // ═══════════════════════════════════════════════
    // ESTADO: PATROL
    // ═══════════════════════════════════════════════

    private void EnterPatrol()
    {
        currentState = EnemyState.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.5f;
        isWaiting = false;

        MoveToNextPatrolPoint();
    }

    private void UpdatePatrol()
    {
        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            float waitTime = Random.Range(minWaitAtWaypoint, maxWaitAtWaypoint);
            patrolWaitCoroutine = StartCoroutine(WaitAtWaypoint(waitTime));
        }
    }

    /// <summary>
    /// Escolhe o próximo destino de patrulha.
    /// Prioriza patrolPoints manuais; usa pontos aleatórios se a lista estiver vazia.
    /// </summary>
    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            // Waypoints manuais
            Transform target = patrolInOrder
                ? patrolPoints[currentPatrolIndex % patrolPoints.Count]
                : patrolPoints[Random.Range(0, patrolPoints.Count)];

            if (patrolInOrder)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;

            agent.SetDestination(target.position);
        }
        else
        {
            // Ponto aleatório validado
            Vector3 dest = GetSafeRandomNavMeshPoint(spawnPosition, wanderRadius);
            agent.SetDestination(dest);
        }
    }

    private IEnumerator WaitAtWaypoint(float waitTime)
    {
        isWaiting = true;
        agent.ResetPath();

        lookAroundCoroutine = StartCoroutine(LookAround(waitTime));
        yield return new WaitForSeconds(waitTime);

        isWaiting = false;

        if (currentState == EnemyState.Patrol)
            MoveToNextPatrolPoint();
    }

    // ═══════════════════════════════════════════════
    // ESTADO: INVESTIGATE
    // ═══════════════════════════════════════════════

    private void EnterInvestigate(Vector3 targetPosition)
    {
        CancelPatrolCoroutines();
        CancelLookAround();

        currentState = EnemyState.Investigate;
        agent.speed = investigateSpeed;
        agent.stoppingDistance = 1f;
        lastKnownPosition = targetPosition;
        investigateTimer = investigateDuration;

        SetInvestigateDestination(targetPosition);
    }

    /// <summary>
    /// Define o destino de investigação.
    /// Se useDoorPoints estiver ativo e houver uma porta relevante no caminho,
    /// o inimigo passa pelo DoorPoint antes de chegar ao destino final.
    /// </summary>
    private void SetInvestigateDestination(Vector3 target)
    {
        if (useDoorPoints && doorPoints != null && doorPoints.Count > 0)
        {
            Transform door = GetRelevantDoorPoint(transform.position, target);
            if (door != null)
            {
                // Navega primeiro até a porta, depois até o destino
                StartCoroutine(NavigateThroughDoor(door.position, target));
                return;
            }
        }

        agent.SetDestination(target);
    }

    /// <summary>
    /// Navega até um DoorPoint e depois continua até o destino final.
    /// </summary>
    private IEnumerator NavigateThroughDoor(Vector3 doorPosition, Vector3 finalTarget)
    {
        agent.SetDestination(doorPosition);

        // Aguarda chegar perto da porta
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.3f)
        {
            if (currentState != EnemyState.Investigate) yield break;
            yield return null;
        }

        // Continua para o destino final
        if (currentState == EnemyState.Investigate)
            agent.SetDestination(finalTarget);
    }

    private void UpdateInvestigate()
    {
        investigateTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isLookingAround)
                lookAroundCoroutine = StartCoroutine(LookAroundThenPatrol());
        }

        if (investigateTimer <= 0f)
        {
            CancelLookAround();
            EnterPatrol();
        }
    }

    private IEnumerator LookAroundThenPatrol()
    {
        isLookingAround = true;
        yield return StartCoroutine(LookAround(Random.Range(2.5f, 4.5f)));
        isLookingAround = false;

        if (currentState == EnemyState.Investigate)
            EnterPatrol();
    }

    // ═══════════════════════════════════════════════
    // ESTADO: CHASE
    // ═══════════════════════════════════════════════

    private void EnterChase()
    {
        CancelPatrolCoroutines();
        CancelLookAround();

        currentState = EnemyState.Chase;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;
    }

    private void UpdateChase()
    {
        if (playerTransform == null) return;

        lastKnownPosition = playerTransform.position;
        agent.SetDestination(lastKnownPosition);
    }

    // ═══════════════════════════════════════════════
    // LOOK AROUND — COM VERIFICAÇÃO DE PAREDES
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Gira o inimigo para os lados de forma humana, verificando antes
    /// se a direção alvo está livre de paredes. Se estiver bloqueada,
    /// escolhe a direção oposta ou para o giro.
    /// </summary>
    private IEnumerator LookAround(float duration)
    {
        // Salva rotação original para evitar rotação cumulativa descontrolada
        float startYaw = transform.eulerAngles.y;
        float targetYaw = startYaw;
        float elapsed = 0f;
        float holdTimer = 0f;
        float holdTime = Random.Range(0.6f, 1.2f);
        bool isHolding = false;

        // Gera uma sequência de ângulos-alvo livres de parede
        float[] offsets = GenerateLookOffsets();
        int offsetIndex = 0;

        if (offsets.Length > 0)
            targetYaw = startYaw + offsets[offsetIndex];

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            holdTimer += Time.deltaTime;

            // Suavemente rotaciona em direção ao ângulo alvo
            float currentYaw = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetYaw,
                lookAroundSpeed * Time.deltaTime
            );
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

            // Chegou perto do alvo: aguarda um momento e escolhe próximo
            bool reachedTarget = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) < 3f;
            if (reachedTarget && !isHolding)
                isHolding = true;

            if (isHolding)
            {
                if (holdTimer >= holdTime)
                {
                    isHolding = false;
                    holdTimer = 0f;
                    holdTime = Random.Range(0.5f, 1.3f);

                    offsetIndex = (offsetIndex + 1) % offsets.Length;
                    targetYaw = startYaw + offsets[offsetIndex];
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// Gera um array de offsets angulares (relativos à rotação atual)
    /// que apontam para direções livres de parede.
    /// </summary>
    private float[] GenerateLookOffsets()
    {
        // Candidatos: esquerda, direita, ligeiramente para cada lado
        float[] candidates = { -maxLookAngle, maxLookAngle, -maxLookAngle * 0.5f, maxLookAngle * 0.5f, 0f };
        var validOffsets = new List<float>();

        foreach (float offset in candidates)
        {
            Vector3 dir = Quaternion.Euler(0f, offset, 0f) * transform.forward;
            if (IsDirectionClear(dir))
                validOffsets.Add(offset);
        }

        // Fallback: se nenhuma direção estiver livre, retorna os dois principais
        if (validOffsets.Count == 0)
            return new float[] { -30f, 30f };

        return validOffsets.ToArray();
    }

    // ═══════════════════════════════════════════════
    // UTILITÁRIOS — NAVMESH E VALIDAÇÃO
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Retorna um ponto aleatório válido na NavMesh que também passa
    /// na validação de distância mínima de paredes.
    /// </summary>
    private Vector3 GetSafeRandomNavMeshPoint(Vector3 center, float radius)
    {
        const int maxAttempts = 15;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            randomPoint.y = center.y;

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                continue;

            if (IsDestinationSafe(hit.position))
                return hit.position;
        }

        // Fallback: retorna o centro (mesmo que não seja ideal)
        return center;
    }

    /// <summary>
    /// Retorna o DoorPoint mais relevante entre a posição atual e o destino.
    /// Relevante = está aproximadamente entre os dois pontos e dentro da
    /// distância de relevância configurada.
    /// Retorna null se não houver nenhum DoorPoint adequado.
    /// </summary>
    private Transform GetRelevantDoorPoint(Vector3 from, Vector3 to)
    {
        Transform bestDoor = null;
        float bestScore = float.MaxValue;
        Vector3 midPoint = (from + to) * 0.5f;

        foreach (Transform door in doorPoints)
        {
            if (door == null) continue;

            // Distância do DoorPoint ao ponto médio do trajeto
            float distToMid = Vector3.Distance(door.position, midPoint);
            float distToPath = DistancePointToSegment(door.position, from, to);

            // Aceita apenas portas próximas ao caminho
            if (distToPath > doorPointRelevanceDistance * 0.5f) continue;
            if (distToMid > doorPointRelevanceDistance) continue;

            // Score: quanto menor, mais relevante
            float score = distToPath + distToMid * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                bestDoor = door;
            }
        }

        return bestDoor;
    }

    /// <summary>
    /// Calcula a distância mínima de um ponto a um segmento de linha.
    /// Usado para avaliar quão próximo um DoorPoint está do trajeto.
    /// </summary>
    private float DistancePointToSegment(Vector3 point, Vector3 segA, Vector3 segB)
    {
        Vector3 ab = segB - segA;
        Vector3 ap = point - segA;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        Vector3 closest = segA + ab * t;
        return Vector3.Distance(point, closest);
    }

    private void CancelPatrolCoroutines()
    {
        if (patrolWaitCoroutine != null)
        {
            StopCoroutine(patrolWaitCoroutine);
            patrolWaitCoroutine = null;
        }
        isWaiting = false;
    }

    private void CancelLookAround()
    {
        if (lookAroundCoroutine != null)
        {
            StopCoroutine(lookAroundCoroutine);
            lookAroundCoroutine = null;
        }
        isLookingAround = false;
    }

    // ═══════════════════════════════════════════════
    // API PÚBLICA
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Chamado quando o Player atira.
    /// O inimigo investiga a região com margem de incerteza.
    /// </summary>
    public void OnPlayerShot(Vector3 shotPosition)
    {
        if (currentState == EnemyState.Chase) return;

        Vector3 noise = Random.insideUnitSphere * shotNoiseRadius;
        noise.y = 0f;
        Vector3 targetPoint = shotPosition + noise;

        EnterInvestigate(targetPoint);
    }

    public bool IsChasing() => currentState == EnemyState.Chase;
    public EnemyState CurrentState => currentState;

    // ═══════════════════════════════════════════════
    // GIZMOS — SOMENTE NO EDITOR
    // ═══════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        DrawVisionCone();
        DrawWallSensors();
        DrawPatrolPoints();
        DrawDoorPoints();
        DrawStateInfo();
        DrawWanderRadius();
    }

    // ── Cone de Visão ──────────────────────────────
    private void DrawVisionCone()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
        Gizmos.DrawSphere(transform.position, viewRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewRadius;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * viewRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);

        Gizmos.color = new Color(0f, 1f, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewRadius);
    }

    // ── Raycasts de Parede ─────────────────────────
    private void DrawWallSensors()
    {
        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 fwd = transform.forward;

        DrawWallRay(pos, fwd, wallCheckDistance, wallFront, "F");
        DrawWallRay(pos, Quaternion.Euler(0f, -90f, 0f) * fwd, wallCheckDistance * 0.7f, wallLeft, "L");
        DrawWallRay(pos, Quaternion.Euler(0f, 90f, 0f) * fwd, wallCheckDistance * 0.7f, wallRight, "R");
        DrawWallRay(pos, Quaternion.Euler(0f, -45f, 0f) * fwd, wallCheckDistance * 0.9f, wallDiagFL, "DL");
        DrawWallRay(pos, Quaternion.Euler(0f, 45f, 0f) * fwd, wallCheckDistance * 0.9f, wallDiagFR, "DR");
    }

    private void DrawWallRay(Vector3 origin, Vector3 dir, float dist, bool hit, string label)
    {
        Gizmos.color = hit ? Color.red : Color.white;
        Gizmos.DrawRay(origin, dir.normalized * dist);
    }

    // ── PatrolPoints ───────────────────────────────
    private void DrawPatrolPoints()
    {
        if (patrolPoints == null) return;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

            // Linha conectando os waypoints em ordem
            if (i > 0 && patrolPoints[i - 1] != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
                Gizmos.DrawLine(patrolPoints[i - 1].position, patrolPoints[i].position);
            }
        }

        // Fecha o ciclo
        if (patrolPoints.Count > 1 &&
            patrolPoints[0] != null &&
            patrolPoints[patrolPoints.Count - 1] != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawLine(patrolPoints[patrolPoints.Count - 1].position, patrolPoints[0].position);
        }
    }

    // ── DoorPoints ─────────────────────────────────
    private void DrawDoorPoints()
    {
        if (doorPoints == null) return;

        foreach (Transform door in doorPoints)
        {
            if (door == null) continue;

            Gizmos.color = new Color(1f, 0.5f, 0f); // laranja
            Gizmos.DrawWireCube(door.position, new Vector3(0.3f, 1.8f, 0.1f));
            Gizmos.DrawWireSphere(door.position, 0.2f);
        }
    }

    // ── Info de Estado ─────────────────────────────
    private void DrawStateInfo()
    {
        if (Application.isPlaying)
        {
            if (currentState == EnemyState.Chase && playerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, playerTransform.position);
                Gizmos.DrawWireSphere(playerTransform.position, 0.4f);
            }

            if (currentState == EnemyState.Investigate)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastKnownPosition);
                Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
            }
        }

        // Ponto de spawn
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? spawnPosition : transform.position,
            0.3f
        );
    }

    // ── Raio de Perambulação ───────────────────────
    private void DrawWanderRadius()
    {
        if (patrolPoints != null && patrolPoints.Count > 0) return; // Não mostra se há waypoints manuais

        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.color = new Color(0f, 0f, 1f, 0.06f);
        Gizmos.DrawSphere(center, wanderRadius);
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}