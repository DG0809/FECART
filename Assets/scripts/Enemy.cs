using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador de inimigo com IA baseada em estados.
/// Estados: Patrol → Investigate → Chase
///
/// Requisitos de configuração no Inspector:
///   - NavMeshAgent  (obrigatório)
///   - CapsuleCollider (obrigatório)
///   - Rigidbody com IsKinematic = true, UseGravity = false (opcional, apenas se precisar de colisão física extra)
///
/// Layers necessárias:
///   - playerLayer   : layer do objeto Player
///   - obstacleLayer : layer de paredes e obstáculos que bloqueiam visão
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // ENUM DE ESTADOS
    // ─────────────────────────────────────────────

    public enum EnemyState
    {
        Patrol,
        Investigate,
        Chase
    }

    // ─────────────────────────────────────────────
    // INSPECTOR — VISÃO
    // ─────────────────────────────────────────────

    [Header("Visão")]
    [Tooltip("Distância máxima de detecção do Player.")]
    [SerializeField] private float viewRadius = 15f;

    [Tooltip("Ângulo total do cone de visão (em graus).")]
    [SerializeField] private float viewAngle = 120f;

    [Tooltip("Layer do Player.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Layer de paredes e obstáculos que bloqueiam a linha de visão.")]
    [SerializeField] private LayerMask obstacleLayer;

    // ─────────────────────────────────────────────
    // INSPECTOR — PATRULHA
    // ─────────────────────────────────────────────

    [Header("Patrulha")]
    [Tooltip("Raio máximo ao redor do ponto de spawn para escolher destinos aleatórios.")]
    [SerializeField] private float wanderRadius = 20f;

    [Tooltip("Velocidade durante a patrulha.")]
    [SerializeField] private float patrolSpeed = 2.5f;

    [Tooltip("Tempo mínimo de espera ao chegar em um ponto de patrulha.")]
    [SerializeField] private float minWaitAtWaypoint = 1.5f;

    [Tooltip("Tempo máximo de espera ao chegar em um ponto de patrulha.")]
    [SerializeField] private float maxWaitAtWaypoint = 4f;

    [Tooltip("Velocidade angular usada ao olhar para os lados durante a patrulha.")]
    [SerializeField] private float lookAroundSpeed = 60f;

    // ─────────────────────────────────────────────
    // INSPECTOR — PERSEGUIÇÃO
    // ─────────────────────────────────────────────

    [Header("Perseguição")]
    [Tooltip("Velocidade durante a perseguição.")]
    [SerializeField] private float chaseSpeed = 5.5f;

    [Tooltip("Distância mínima para o inimigo parar ao perseguir o Player.")]
    [SerializeField] private float attackRange = 1.5f;

    // ─────────────────────────────────────────────
    // INSPECTOR — INVESTIGAÇÃO
    // ─────────────────────────────────────────────

    [Header("Investigação")]
    [Tooltip("Velocidade ao correr para o ponto de investigação.")]
    [SerializeField] private float investigateSpeed = 4f;

    [Tooltip("Tempo total que o inimigo investiga antes de voltar à patrulha.")]
    [SerializeField] private float investigateDuration = 6f;

    [Tooltip("Raio de incerteza adicionado à posição do disparo (simula que o inimigo não sabe a posição exata).")]
    [SerializeField] private float shotNoiseRadius = 3f;

    // ─────────────────────────────────────────────
    // REFERÊNCIAS PRIVADAS
    // ─────────────────────────────────────────────

    private NavMeshAgent agent;
    private Transform playerTransform;
    private Vector3 spawnPosition;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private EnemyState currentState = EnemyState.Patrol;

    // Patrol
    private bool isWaiting = false;
    private Coroutine patrolWaitCoroutine;

    // Investigate
    private Vector3 lastKnownPosition;
    private float investigateTimer;

    // Look Around (olhar para os lados)
    private bool isLookingAround = false;
    private Coroutine lookAroundCoroutine;

    // ─────────────────────────────────────────────
    // UNITY — INICIALIZAÇÃO
    // ─────────────────────────────────────────────

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;
    }

    private void Start()
    {
        // Busca o Player pela tag, evitando depender de um script específico.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("[Enemy] Player não encontrado. Certifique-se que o objeto Player tem a tag 'Player'.");

        // Garante que o agente não suba paredes ou flutue por conta do NavMesh.
        agent.updateRotation = true;
        agent.updateUpAxis = false; // Impede rotação no eixo Y que poderia causar flutuação.

        EnterPatrol();
    }

    private void Update()
    {
        CheckPlayerVisibility();
        UpdateStateMachine();
    }

    // ─────────────────────────────────────────────
    // DETECÇÃO DO PLAYER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Verifica se o Player está dentro do cone de visão e sem obstáculos no caminho.
    /// Transiciona para Chase ou Investigate conforme o resultado.
    /// </summary>
    private void CheckPlayerVisibility()
    {
        if (playerTransform == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.6f; // Origem do raycast na altura dos "olhos"
        Vector3 toPlayer = playerTransform.position - origin;
        float distance = toPlayer.magnitude;
        Vector3 toPlayerNorm = toPlayer.normalized;

        // 1. Verificar distância
        if (distance > viewRadius)
        {
            OnPlayerLost();
            return;
        }

        // 2. Verificar ângulo do cone de visão
        float angle = Vector3.Angle(transform.forward, toPlayerNorm);
        if (angle > viewAngle * 0.5f)
        {
            OnPlayerLost();
            return;
        }

        // 3. Verificar linha de visão (paredes e obstáculos)
        if (Physics.Raycast(origin, toPlayerNorm, distance, obstacleLayer))
        {
            OnPlayerLost();
            return;
        }

        // Player visível: iniciar perseguição
        OnPlayerSpotted();
    }

    /// <summary>
    /// Chamado quando o Player é avistado.
    /// </summary>
    private void OnPlayerSpotted()
    {
        lastKnownPosition = playerTransform.position;

        if (currentState != EnemyState.Chase)
            EnterChase();
    }

    /// <summary>
    /// Chamado quando o Player sai do cone de visão.
    /// </summary>
    private void OnPlayerLost()
    {
        // Só transiciona se estava perseguindo.
        if (currentState == EnemyState.Chase)
            EnterInvestigate(lastKnownPosition);
    }

    // ─────────────────────────────────────────────
    // MÁQUINA DE ESTADOS — DESPACHANTE
    // ─────────────────────────────────────────────

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Chase: UpdateChase(); break;
        }
    }

    // ─────────────────────────────────────────────
    // ESTADO: PATROL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Configura o inimigo para o estado de patrulha.
    /// </summary>
    private void EnterPatrol()
    {
        currentState = EnemyState.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.5f;
        isWaiting = false;

        MoveToRandomWaypoint();
    }

    /// <summary>
    /// Atualizado a cada frame enquanto o inimigo está em patrulha.
    /// Quando chega ao destino, inicia a espera com olhar lateral.
    /// </summary>
    private void UpdatePatrol()
    {
        if (isWaiting) return;

        // Chegou ao destino?
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            float waitTime = Random.Range(minWaitAtWaypoint, maxWaitAtWaypoint);
            patrolWaitCoroutine = StartCoroutine(WaitAndChooseNextWaypoint(waitTime));
        }
    }

    /// <summary>
    /// Escolhe um ponto aleatório válido na NavMesh e move o agente até lá.
    /// </summary>
    private void MoveToRandomWaypoint()
    {
        Vector3 destination = GetRandomNavMeshPoint(spawnPosition, wanderRadius);
        agent.SetDestination(destination);
    }

    /// <summary>
    /// Aguarda um tempo, olha para os lados simulando inspeção e depois escolhe o próximo waypoint.
    /// </summary>
    private IEnumerator WaitAndChooseNextWaypoint(float waitTime)
    {
        isWaiting = true;
        agent.ResetPath(); // Para o agente no lugar

        // Olhar para os lados enquanto espera
        lookAroundCoroutine = StartCoroutine(LookAround(waitTime));
        yield return new WaitForSeconds(waitTime);

        isWaiting = false;

        // Se ainda estiver em patrulha, escolhe novo destino
        if (currentState == EnemyState.Patrol)
            MoveToRandomWaypoint();
    }

    // ─────────────────────────────────────────────
    // ESTADO: INVESTIGATE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Configura o inimigo para o estado de investigação com base em uma posição alvo.
    /// </summary>
    private void EnterInvestigate(Vector3 targetPosition)
    {
        // Cancela coroutines de patrulha pendentes
        CancelPatrolCoroutines();

        currentState = EnemyState.Investigate;
        agent.speed = investigateSpeed;
        agent.stoppingDistance = 1f;
        lastKnownPosition = targetPosition;
        investigateTimer = investigateDuration;

        agent.SetDestination(lastKnownPosition);
    }

    /// <summary>
    /// Atualizado a cada frame durante a investigação.
    /// Quando chega ao ponto, olha ao redor antes de voltar à patrulha.
    /// </summary>
    private void UpdateInvestigate()
    {
        investigateTimer -= Time.deltaTime;

        // Chegou ao ponto de investigação?
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isLookingAround)
                lookAroundCoroutine = StartCoroutine(LookAroundThenPatrol());
        }

        // Tempo de investigação esgotado → volta à patrulha
        if (investigateTimer <= 0f)
        {
            CancelLookAround();
            EnterPatrol();
        }
    }

    /// <summary>
    /// Olha para os lados por alguns segundos e depois inicia a patrulha.
    /// </summary>
    private IEnumerator LookAroundThenPatrol()
    {
        isLookingAround = true;
        float lookTime = Random.Range(2f, 4f);
        yield return StartCoroutine(LookAround(lookTime));
        isLookingAround = false;

        if (currentState == EnemyState.Investigate)
            EnterPatrol();
    }

    // ─────────────────────────────────────────────
    // ESTADO: CHASE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Configura o inimigo para o estado de perseguição.
    /// </summary>
    private void EnterChase()
    {
        CancelPatrolCoroutines();
        CancelLookAround();

        currentState = EnemyState.Chase;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;
    }

    /// <summary>
    /// Atualizado a cada frame durante a perseguição.
    /// Atualiza o destino continuamente para o Player.
    /// </summary>
    private void UpdateChase()
    {
        if (playerTransform == null) return;

        lastKnownPosition = playerTransform.position;
        agent.SetDestination(lastKnownPosition);
    }

    // ─────────────────────────────────────────────
    // COMPORTAMENTO: OLHAR PARA OS LADOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gira o inimigo suavemente para os lados para simular um comportamento humano de busca.
    /// Usa a rotação do Transform diretamente enquanto o agente está parado.
    /// </summary>
    private IEnumerator LookAround(float duration)
    {
        float elapsed = 0f;
        float direction = 1f;
        float switchInterval = Random.Range(0.8f, 1.5f);
        float switchTimer = switchInterval;

        while (elapsed < duration)
        {
            switchTimer -= Time.deltaTime;
            if (switchTimer <= 0f)
            {
                direction = -direction;
                switchTimer = Random.Range(0.8f, 1.5f);
            }

            // Gira somente no eixo Y
            transform.Rotate(0f, direction * lookAroundSpeed * Time.deltaTime, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA — EVENTOS EXTERNOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Chamado externamente quando o Player atira.
    /// O inimigo investiga a região do disparo com uma margem de incerteza.
    /// </summary>
    /// <param name="shotPosition">Posição de origem do disparo.</param>
    public void OnPlayerShot(Vector3 shotPosition)
    {
        // Só investiga se não estiver já perseguindo
        if (currentState == EnemyState.Chase) return;

        // Adiciona incerteza para que o inimigo não saiba a posição exata
        Vector3 noiseOffset = Random.insideUnitSphere * shotNoiseRadius;
        noiseOffset.y = 0f;
        Vector3 targetPoint = shotPosition + noiseOffset;

        EnterInvestigate(targetPoint);
    }

    /// <summary>
    /// Retorna verdadeiro se o inimigo está ativamente perseguindo o Player.
    /// </summary>
    public bool IsChasing() => currentState == EnemyState.Chase;

    /// <summary>
    /// Retorna o estado atual do inimigo.
    /// </summary>
    public EnemyState CurrentState => currentState;

    // ─────────────────────────────────────────────
    // UTILITÁRIOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Retorna um ponto aleatório válido na NavMesh dentro do raio especificado.
    /// Faz múltiplas tentativas para garantir que encontra um ponto válido.
    /// </summary>
    private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            randomPoint.y = center.y; // Mantém na altura do centro para evitar pontos no ar

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }

        // Fallback: retorna o próprio centro caso nenhum ponto seja encontrado
        return center;
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

    // ─────────────────────────────────────────────
    // GIZMOS — SOMENTE NO EDITOR
    // ─────────────────────────────────────────────

    /// <summary>
    /// Desenha o campo de visão e informações de debug no Editor.
    /// Executado apenas quando o objeto está selecionado na hierarquia.
    /// NUNCA deve ser chamado de dentro do Update().
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        DrawVisionCone();
        DrawStateInfo();
    }

    private void DrawVisionCone()
    {
        // Esfera de raio de visão
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, viewRadius);

        // Contorno da esfera
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Linhas laterais do cone de visão
        Vector3 leftBoundary = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewRadius;
        Vector3 rightBoundary = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * viewRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Linha de direção frontal
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewRadius);
    }

    private void DrawStateInfo()
    {
        // Linha vermelha até o Player quando perseguindo
        if (currentState == EnemyState.Chase && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            Gizmos.DrawWireSphere(playerTransform.position, 0.4f);
        }

        // Linha magenta até a última posição conhecida quando investigando
        if (currentState == EnemyState.Investigate)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lastKnownPosition);
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }

        // Ponto do spawn
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPosition, 0.3f);

        // Raio de perambulação
        Gizmos.color = new Color(0f, 0f, 1f, 0.08f);
        Gizmos.DrawSphere(spawnPosition, wanderRadius);
    }
}
