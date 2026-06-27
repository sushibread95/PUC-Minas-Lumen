using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyCombatMode
{
    Melee,
    Ranged,
    Grabber
}

public enum EnemyPatrolMode
{
    Stationary,
    Waypoints,
    RandomNavMesh
}

public enum RangedProjectileFireMode
{
    DirectInstantiate,
    CannonFire,
    CannonThenDirectFallback
}

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CorruptedNPC))]
[RequireComponent(typeof(LockOnTarget))]
[DisallowMultipleComponent]
public class EnemyAIController : MonoBehaviour
{
    #region Inspector - Core References

    [Header("REFERÊNCIAS PRINCIPAIS")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyHealth enemyHealth;
    [HideInInspector] public CorruptedNPC npcData;
    [HideInInspector] public LockOnTarget lockOnTarget;
    [SerializeField] private EnemyIdentity identity;

    [Header("ANIMAÇÃO")]
    public Animator animator;
    public string speedParam = "Speed";
    [SerializeField] private string alertBoolParam = "IsAlert";
    [SerializeField] private bool useAlertBoolParam = true;

    private int speedHash;
    private int alertBoolHash;
    private bool hasSpeedParam;
    private bool hasAlertBoolParam;

    [Header("ANIMAÇÃO - ATAQUE")]
    [Tooltip("Trigger usado para chamar a animação de ataque melee. Ex.: Punch.")]
    [SerializeField] private string meleeAttackTrigger = "Punch";

    #endregion

    #region Inspector - Type

    [Header("TIPO DE INIMIGO")]
    [Tooltip("Define se este inimigo luta corpo a corpo, à distância ou tenta agarrar o player.")]
    [SerializeField] private EnemyCombatMode combatMode = EnemyCombatMode.Melee;

    // #1: isPurified deixou de ser uma cópia local. Agora é DERIVADO da fonte
    // única (o estado narrativo do CorruptedNPC), eliminando a duplicação de estado.
    public bool isPurified => npcData != null && npcData.currentState == NPCState.Purificado;

    #endregion

    #region Inspector - Detection

    [Header("PERCEPÇÃO")]
    public float sightRange = 15f;
    public float hearingRange = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    [SerializeField] private Transform eyePoint;
    public LayerMask targetMask;
    public LayerMask obstructionMask;
    [SerializeField] private float perceptionInterval = 0.12f;
    [SerializeField] private float closeDetectionRadius = 2.25f;
    [SerializeField] private bool requireLineOfSight = true;

    #endregion

    #region Inspector - Movement And Patrol

    [Header("MOVIMENTO")]
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float repathInterval = 0.18f;
    [Tooltip("Faz o inimigo olhar para a direção real do movimento, evitando andar de lado na patrulha.")]
    [SerializeField] private bool faceMovementDirection = true;
    [Tooltip("Velocidade mínima para o inimigo rotacionar para a direção do movimento.")]
    [SerializeField] private float faceMovementMinSpeed = 0.08f;

    [Header("PATRULHA")]
    [SerializeField] private EnemyPatrolMode patrolMode = EnemyPatrolMode.Waypoints;
    public Transform[] waypoints;
    [SerializeField] private float waypointReachDistance = 0.8f;
    [SerializeField] private float waypointWaitTime = 2.0f;
    [SerializeField] private float randomPatrolRadius = 7f;
    [SerializeField] private float randomPatrolWaitTime = 2.5f;
    [SerializeField] private bool rotateWhileStationary = true;

    #endregion

    #region Inspector - Combat

    [Header("COMBATE GERAL")]
    public float combatAggroRange = 7f;
    public float combatMemoryDuration = 3.0f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2.0f;
    [SerializeField] private float stopBuffer = 0.25f;
    [SerializeField] private bool keepFacingTargetInCombat = true;

    [Header("MELEE / GRAB")]
    public EnemyMeleeHitbox meleeHitbox;
    [Tooltip("Colliders dentro dos objetos vazios usados como hitbox do ataque. O script liga/desliga o GameObject desses colliders durante o golpe.")]
    [SerializeField] private Collider[] meleeAttackColliders;
    [Tooltip("Se marcado, ativa/desativa o objeto vazio da hitbox em vez de apenas ligar/desligar o Collider. Recomendado para hitboxes em objetos filhos vazios.")]
    [SerializeField] private bool activateMeleeHitboxObjects = true;
    [Tooltip("Se marcado, o script adiciona automaticamente EnemyAttackHitbox nos colliders configurados.")]
    [SerializeField] private bool autoSetupMeleeAttackColliders = true;
    [Tooltip("Se marcado, força os colliders de ataque configurados como Trigger.")]
    [SerializeField] private bool autoSetMeleeCollidersAsTrigger = true;
    [Tooltip("Se marcado, adiciona Rigidbody cinemático nos objetos da hitbox para garantir eventos de trigger.")]
    [SerializeField] private bool ensureKinematicRigidbodyOnHitbox = true;
    [Tooltip("Se a lista estiver vazia, tenta achar colliders filhos com nomes contendo Hitbox, Punch ou Attack.")]
    [SerializeField] private bool autoFindMeleeCollidersIfEmpty = true;
    [Tooltip("Máscara de layers que podem receber dano do ataque melee.")]
    [SerializeField] private LayerMask meleeDamageMask = ~0;
    [Tooltip("Se marcado, o ataque só tenta causar dano em objetos ligados ao Player.")]
    [SerializeField] private bool damagePlayerOnly = true;
    [Tooltip("Mostra logs de ativação e colisão da hitbox melee.")]
    [SerializeField] private bool debugMeleeHitbox = false;
    [Tooltip("Se marcado, cada janela de ataque só pode causar dano uma vez no Player.")]
    [SerializeField] private bool onePlayerHitPerAttackWindow = true;
    [Tooltip("Se marcado, fecha a hitbox imediatamente depois de acertar o Player.")]
    [SerializeField] private bool closeHitboxAfterSuccessfulHit = true;
    [Tooltip("Desliga os colliders de ataque ao iniciar para evitar dano permanente.")]
    [SerializeField] private bool autoDisableMeleeCollidersOnStart = true;
    [HideInInspector] public string attackTrigger = "Attack";
    public float meleeDamage = 15f;
    [Tooltip("Tempo após o início da animação para ligar o dano.")]
    public float attackImpactDelay = 0.4f;
    [Tooltip("Tempo em que a hitbox fica ativa.")]
    public float hitboxActiveDuration = 0.2f;
    public EnemyGrabber grabberComponent;

    [Header("RANGED - DISPARO")]
    [Tooltip("Projéteis/magias que o inimigo ranged pode disparar. Normalmente use apenas o Element 0.")]
    public Spell[] enemyAttacks;
    [Tooltip("Componente que cria/dispara o projétil. Configure o ponto de saída no próprio Cannon.")]
    [SerializeField] private Cannon cannon;
    [Tooltip("Modo de disparo. Direct Instantiate ignora dependências internas do Cannon e instancia o prefab do Spell diretamente.")]
    [SerializeField] private RangedProjectileFireMode rangedProjectileFireMode = RangedProjectileFireMode.DirectInstantiate;
    [Tooltip("Ponto onde o projétil nasce. Se vazio, tenta achar FirePoint/Muzzle/ProjectileSpawn/Cannon nos filhos, depois usa o Cannon ou o próprio inimigo.")]
    [SerializeField] private Transform rangedProjectileSpawnPoint;
    [Tooltip("Se marcado, tenta localizar automaticamente um ponto de disparo nos filhos do inimigo.")]
    [SerializeField] private bool autoFindRangedProjectileSpawnPoint = true;
    [Tooltip("Distância extra à frente do ponto de disparo para evitar nascer dentro do inimigo.")]
    [SerializeField] private float projectileSpawnForwardOffset = 0.15f;
    [Tooltip("Se maior que zero, aplica velocidade inicial no Rigidbody do projétil instanciado diretamente.")]
    [SerializeField] private float directProjectileLaunchSpeed = 0f;
    [Tooltip("Se marcado, tenta configurar owner/target do projétil criado diretamente usando métodos/campos comuns do projeto.")]
    [SerializeField] private bool configureDirectProjectile = true;
    [Tooltip("Trigger da animação de ataque ranged. Use o mesmo nome do parâmetro no Animator, por padrão: Ranged.")]
    [SerializeField] private string rangedAttackTrigger = "Ranged";
    [Tooltip("Tempo, em segundos, depois do início do ataque para spawnar/disparar o projétil.")]
    [SerializeField] private float rangedProjectileReleaseDelay = 0.35f;
    [Tooltip("Duração total aproximada do ataque. Depois desse tempo, a IA volta a se mover/agir.")]
    [SerializeField] private float rangedAttackLockDuration = 0.85f;
    [Tooltip("Se marcado, o inimigo só inicia o ataque quando tem linha de visão direta para o player.")]
    [SerializeField] private bool rangedNeedsLineOfSight = true;
    [Tooltip("Se marcado, cancela o projétil se perder linha de visão exatamente no frame do disparo. Recomendado deixar desligado para o projétil sempre sair junto da animação.")]
    [SerializeField] private bool recheckLineOfSightOnProjectileRelease = false;
    [Tooltip("Se marcado, mostra avisos no Console quando faltar Cannon, Spell ou projectile.")]
    [SerializeField] private bool showRangedSetupWarnings = true;

    [Header("RANGED - DISTÂNCIA")]
    [Tooltip("Distância máxima para o inimigo ranged atacar.")]
    [SerializeField] private float rangedAttackRange = 10f;
    [Tooltip("Distância que o inimigo tenta manter do player.")]
    [SerializeField] private float rangedIdealDistance = 7f;
    [Tooltip("Se o player chegar mais perto que isso, o inimigo tenta recuar.")]
    [SerializeField] private float rangedMinDistance = 4f;
    [Tooltip("Quanto o inimigo tenta recuar quando o player está perto demais.")]
    [SerializeField] private float rangedRetreatDistance = 2.5f;

    [HideInInspector] [SerializeField] private bool randomizeRangedAttack = false;
    [HideInInspector] [SerializeField] private bool debugRangedAttack = false;

    [Header("NOCAUTE")]
    public float fallenDuration = 15f;

    #endregion

    #region Runtime State

    private IEnemyState currentState;
    public EnemyStateID currentStateID = EnemyStateID.Idle;

    [HideInInspector] public Transform playerTarget;
    [HideInInspector] public Vector3 lastSeenLocation;

    private int currentWaypointIndex;
    private float waitTimer;
    private float attackTimer;
    private float loseSightTimer;
    private float nextPerceptionTime;
    private float nextRepathTime;
    private bool hasRecentSight;
    private bool hasRecentHearing;
    private bool isAttacking;
    private bool meleeDamageWindowActive;
    private bool playerHitRegisteredThisWindow;
    private int meleeAttackWindowId;
    private readonly HashSet<Transform> damagedTargetsThisAttack = new HashSet<Transform>();
    private Vector3 spawnPosition;

    #endregion

    #region Public API

    public EnemyCombatMode CombatMode => combatMode;
    public bool IsBusyAttacking => isAttacking;

    #endregion

    #region Unity Lifecycle

    private void Reset()
    {
        CacheComponents();
        eyePoint = transform;
    }

    private void Awake()
    {
        CacheComponents();
        CacheAnimatorParameters();
        spawnPosition = transform.position;
    }

    private void OnValidate()
    {
        // O controle principal da hitbox agora é feito pelo Inspector com Delay/Duração, não por Animation Events.
    }

    private void Start()
    {
        SetupAgent();
        SetupMeleeAttackColliders();

        if (autoDisableMeleeCollidersOnStart)
            SetMeleeAttackCollidersActive(false);

        StartCoroutine(FindPlayerRoutine());
        ChangeState(GetInitialState());
    }

    private void Update()
    {
        if (ShouldSkipAI())
            return;

        UpdateAnimatorMovement();
        RefreshPerceptionIfNeeded();

        if (enemyHealth != null && enemyHealth.isFallen && currentStateID != EnemyStateID.Fallen && currentStateID != EnemyStateID.Dead)
            ChangeState(EnemyStateID.Fallen);

        currentState?.UpdateState();
        UpdateFacingDirection();
    }

    private void FixedUpdate()
    {
        if (ShouldSkipAI())
            return;

        currentState?.FixedUpdateState();
    }

    #endregion

    #region Setup

    // Centraliza cache de componentes para evitar referências nulas em prefabs diferentes.
    private void CacheComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (npcData == null) npcData = GetComponent<CorruptedNPC>();
        if (lockOnTarget == null) lockOnTarget = GetComponent<LockOnTarget>();
        if (identity == null) identity = GetComponent<EnemyIdentity>();
        if (identity == null) identity = gameObject.AddComponent<EnemyIdentity>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>(true);
        if (grabberComponent == null) grabberComponent = GetComponent<EnemyGrabber>();
    }

    // Cacheia parâmetros existentes para evitar warnings quando o Animator não tiver algum parâmetro opcional.
    private void CacheAnimatorParameters()
    {
        speedHash = !string.IsNullOrEmpty(speedParam) ? Animator.StringToHash(speedParam) : 0;
        alertBoolHash = !string.IsNullOrEmpty(alertBoolParam) ? Animator.StringToHash(alertBoolParam) : 0;

        hasSpeedParam = HasAnimatorParameter(speedParam, AnimatorControllerParameterType.Float);
        hasAlertBoolParam = useAlertBoolParam && HasAnimatorParameter(alertBoolParam, AnimatorControllerParameterType.Bool);
    }

    // Confere se o parâmetro existe antes de chamar SetFloat/SetBool.
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == expectedType)
                return true;
        }

        return false;
    }

    // Ajusta o NavMeshAgent para parar em distâncias adequadas por tipo de inimigo.
    private void SetupAgent()
    {
        if (agent == null)
            return;

        agent.speed = walkSpeed;
        agent.stoppingDistance = GetStoppingDistanceForMode();
        agent.autoBraking = true;
        agent.updatePosition = true;
        agent.updateRotation = false;
    }



    // Mantido apenas para compatibilidade com versões antigas. O ataque atual não depende de Animation Events.
    private void SetupAnimationEventRelay()
    {
    }

    private void SetupMeleeAttackColliders()
    {
        if (autoFindMeleeCollidersIfEmpty && (meleeAttackColliders == null || meleeAttackColliders.Length == 0))
            meleeAttackColliders = FindNamedAttackCollidersInChildren();

        if (!autoSetupMeleeAttackColliders || meleeAttackColliders == null)
            return;

        for (int i = 0; i < meleeAttackColliders.Length; i++)
        {
            Collider attackCollider = meleeAttackColliders[i];
            if (attackCollider == null)
                continue;

            if (autoSetMeleeCollidersAsTrigger)
                attackCollider.isTrigger = true;

            if (ensureKinematicRigidbodyOnHitbox)
                EnsureKinematicRigidbody(attackCollider.gameObject);

            EnemyAttackHitbox relay = attackCollider.GetComponent<EnemyAttackHitbox>();
            if (relay == null)
                relay = attackCollider.gameObject.AddComponent<EnemyAttackHitbox>();

            relay.Initialize(this);

            // Mantém o collider pronto, mas desliga o objeto/colisor até a janela de dano.
            SetSingleMeleeAttackObjectActive(attackCollider, false);
        }
    }

    // Tenta achar hitboxes por nome sem pegar colliders de corpo comuns.
    private Collider[] FindNamedAttackCollidersInChildren()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        List<Collider> result = new List<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.transform == transform)
                continue;

            string lowerName = col.name.ToLowerInvariant();
            if (lowerName.Contains("hitbox") || lowerName.Contains("punch") || lowerName.Contains("attack"))
                result.Add(col);
        }

        return result.ToArray();
    }

    // Garante que o collider trigger receba eventos mesmo estando em objeto filho vazio.
    private void EnsureKinematicRigidbody(GameObject hitboxObject)
    {
        if (hitboxObject == null)
            return;

        Rigidbody rb = hitboxObject.GetComponent<Rigidbody>();
        if (rb == null)
            rb = hitboxObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private EnemyStateID GetInitialState()
    {
        return patrolMode == EnemyPatrolMode.Stationary ? EnemyStateID.Idle : EnemyStateID.Patrol;
    }

    private IEnumerator FindPlayerRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        while (playerTarget == null && enabled)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                yield break;
            }

            yield return wait;
        }
    }

    #endregion

    #region State Machine

    public void ChangeState(EnemyStateID newStateID)
    {
        if (currentStateID == newStateID)
            return;

        currentState?.ExitState();
        currentState = GetStateInstance(newStateID);
        currentStateID = newStateID;
        currentState?.EnterState();
    }

    private IEnemyState GetStateInstance(EnemyStateID stateID)
    {
        return stateID switch
        {
            EnemyStateID.Idle => new PatrolState(this),
            EnemyStateID.Patrol => new PatrolState(this),
            EnemyStateID.Alert => new AlertState(this),
            EnemyStateID.Combat => new CombatState(this),
            EnemyStateID.Fallen => new FallenState(this),
            EnemyStateID.Dead => new DeadState(this),
            _ => new PatrolState(this),
        };
    }

    private bool ShouldSkipAI()
    {
        return isPurified || enemyHealth == null || enemyHealth.isDead || playerTarget == null || agent == null || !agent.enabled;
    }

    #endregion

    #region Perception

    // Evita raycasts a cada frame e mantém visão/audição estáveis.
    private void RefreshPerceptionIfNeeded()
    {
        if (Time.time < nextPerceptionTime)
            return;

        nextPerceptionTime = Time.time + Mathf.Max(0.02f, perceptionInterval);
        hasRecentSight = EvaluateSight();
        hasRecentHearing = EvaluateHearing();
    }

    public bool CanSeeTarget()
    {
        RefreshPerceptionIfNeeded();
        return hasRecentSight;
    }

    public bool CanHearTarget()
    {
        RefreshPerceptionIfNeeded();
        return hasRecentHearing;
    }

    private bool EvaluateSight()
    {
        if (playerTarget == null)
            return false;

        Vector3 eyePosition = GetEyePosition();
        Vector3 targetPosition = GetTargetFocusPosition();
        Vector3 toTarget = targetPosition - eyePosition;
        float distance = toTarget.magnitude;

        float currentSightRange = GetCurrentSightRange();
        if (distance > currentSightRange)
            return false;

        if (distance > closeDetectionRadius)
        {
            Vector3 direction = toTarget.normalized;
            float minDot = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
            if (Vector3.Dot(transform.forward, direction) < minDot)
                return false;
        }

        if (requireLineOfSight && IsLineBlocked(eyePosition, targetPosition))
            return false;

        lastSeenLocation = playerTarget.position;
        return true;
    }

    private bool EvaluateHearing()
    {
        if (playerTarget == null)
            return false;

        PlayerControllerSystem playerController = playerTarget.GetComponent<PlayerControllerSystem>();
        if (playerController == null)
            return false;

        float noiseMultiplier = Mathf.Max(0f, playerController.noiseLevel);
        float effectiveRange = hearingRange * noiseMultiplier;
        if (effectiveRange < 0.1f)
            return false;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > effectiveRange)
            return false;

        lastSeenLocation = playerTarget.position;
        return true;
    }

    private bool IsLineBlocked(Vector3 start, Vector3 end)
    {
        if (obstructionMask.value == 0)
            return false;

        if (!Physics.Linecast(start, end, out RaycastHit hit, obstructionMask, QueryTriggerInteraction.Ignore))
            return false;

        if (playerTarget != null && hit.transform.IsChildOf(playerTarget))
            return false;

        return true;
    }

    private Vector3 GetEyePosition()
    {
        return eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
    }

    private Vector3 GetTargetFocusPosition()
    {
        if (playerTarget == null)
            return transform.position;

        return playerTarget.position + Vector3.up * 1f;
    }

    private float GetCurrentSightRange()
    {
        float range = sightRange;
        PlayerControllerSystem playerController = playerTarget != null ? playerTarget.GetComponent<PlayerControllerSystem>() : null;
        if (playerController != null)
            range *= Mathf.Max(0.15f, playerController.visibilityFactor);

        return range;
    }

    public bool HasDirectLineToTarget()
    {
        if (playerTarget == null)
            return false;

        return !IsLineBlocked(GetEyePosition(), GetTargetFocusPosition());
    }

    #endregion

    #region Patrol

    // Executa patrulha por pontos, guarda parado ou patrulha aleatória na NavMesh.
    public void UpdatePatrol()
    {
        if (CanSeeTarget() || CanHearTarget())
        {
            ChangeState(EnemyStateID.Alert);
            return;
        }

        if (patrolMode == EnemyPatrolMode.Stationary || currentStateID == EnemyStateID.Idle)
        {
            StopAgent();
            if (rotateWhileStationary)
                transform.Rotate(0f, 25f * Time.deltaTime, 0f);
            return;
        }

        if (patrolMode == EnemyPatrolMode.RandomNavMesh)
        {
            UpdateRandomPatrol();
            return;
        }

        UpdateWaypointPatrol();
    }

    private void UpdateWaypointPatrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            StopAgent();
            return;
        }

        ResumeAgent(walkSpeed, 0.1f);

        if (!HasValidPathOrPending())
            SetDestinationSafe(waypoints[currentWaypointIndex].position);

        if (HasReachedDestination(waypointReachDistance))
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                SetDestinationSafe(waypoints[currentWaypointIndex].position);
                waitTimer = 0f;
            }
        }
    }

    private void UpdateRandomPatrol()
    {
        ResumeAgent(walkSpeed, 0.1f);

        if (!HasValidPathOrPending() || HasReachedDestination(waypointReachDistance))
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= randomPatrolWaitTime)
            {
                if (TryGetRandomNavMeshPoint(spawnPosition, randomPatrolRadius, out Vector3 point))
                    SetDestinationSafe(point);

                waitTimer = 0f;
            }
        }
    }

    private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 random = Random.insideUnitCircle * Mathf.Max(0.5f, radius);
            Vector3 candidate = center + new Vector3(random.x, 0f, random.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    #endregion

    #region Alert

    // Investiga a última posição vista/ouvida antes de retornar à patrulha.
    public void UpdateAlert(ref float alertTimer, ref Vector3 investigationPoint, float maxAlertDuration)
    {
        if (CanSeeTarget())
        {
            ChangeState(EnemyStateID.Combat);
            return;
        }

        ResumeAgent(walkSpeed, 0.1f);

        if (investigationPoint == Vector3.zero)
            investigationPoint = lastSeenLocation;

        if (CanHearTarget() && Vector3.Distance(lastSeenLocation, investigationPoint) > 0.35f)
        {
            investigationPoint = lastSeenLocation;
            alertTimer = 0f;
            SetDestinationSafe(investigationPoint);
        }

        if (!HasValidPathOrPending())
            SetDestinationSafe(investigationPoint);

        if (HasReachedDestination(1f))
        {
            StopAgent();
            transform.Rotate(0f, 70f * Time.deltaTime, 0f);
            alertTimer += Time.deltaTime;
        }

        if (alertTimer >= maxAlertDuration)
            ChangeState(GetInitialState());
    }

    #endregion

    #region Combat

    // Atualiza o combate conforme o tipo do inimigo.
    public void UpdateCombat(ref float externalAttackTimer, ref float externalLoseSightTimer)
    {
        if (playerTarget == null)
        {
            ChangeState(GetInitialState());
            return;
        }

        attackTimer += Time.deltaTime;
        externalAttackTimer = attackTimer;

        bool canSee = CanSeeTarget();
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (!canSee)
        {
            externalLoseSightTimer += Time.deltaTime;
            MoveToLastSeenLocation();

            if (externalLoseSightTimer >= combatMemoryDuration)
                ChangeState(EnemyStateID.Alert);

            return;
        }

        externalLoseSightTimer = 0f;
        lastSeenLocation = playerTarget.position;

        switch (combatMode)
        {
            case EnemyCombatMode.Ranged:
                UpdateRangedCombat(distance);
                break;
            case EnemyCombatMode.Grabber:
            case EnemyCombatMode.Melee:
            default:
                UpdateMeleeCombat(distance);
                break;
        }
    }

    private void UpdateMeleeCombat(float distance)
    {
        float stopDistance = Mathf.Max(0.05f, attackRange - stopBuffer);

        if (distance <= attackRange)
        {
            StopAgent();
            FaceTarget();

            if (attackTimer >= attackCooldown && !isAttacking)
            {
                PerformMeleeAttack();
                attackTimer = 0f;
            }

            return;
        }

        ResumeAgent(chaseSpeed, stopDistance);
        SetDestinationThrottled(playerTarget.position);
        if (keepFacingTargetInCombat)
            FaceTarget();
    }

    private void UpdateRangedCombat(float distance)
    {
        float effectiveAttackRange = Mathf.Max(attackRange, rangedAttackRange);
        bool canShoot = !rangedNeedsLineOfSight || HasDirectLineToTarget();

        if (distance < rangedMinDistance)
        {
            RetreatFromTarget();
            FaceTarget();
            return;
        }

        if (distance <= effectiveAttackRange && canShoot)
        {
            StopAgent();
            FaceTarget();

            if (attackTimer >= attackCooldown && !isAttacking)
            {
                StartCoroutine(RangedAttackRoutine());
                attackTimer = 0f;
            }

            return;
        }

        float desiredStopDistance = Mathf.Max(0.1f, rangedIdealDistance);
        ResumeAgent(chaseSpeed, desiredStopDistance);
        SetDestinationThrottled(playerTarget.position);
        if (keepFacingTargetInCombat)
            FaceTarget();
    }

    public void PerformMeleeAttack()
    {
        if (isAttacking)
            return;

        string triggerToUse = !string.IsNullOrEmpty(meleeAttackTrigger) ? meleeAttackTrigger : attackTrigger;
        if (animator != null && !string.IsNullOrEmpty(triggerToUse))
            animator.SetTrigger(triggerToUse);

        if (combatMode == EnemyCombatMode.Grabber && grabberComponent != null)
        {
            grabberComponent.StartGrabAttempt();
            StartCoroutine(AttackCooldownLock(Mathf.Max(attackImpactDelay + hitboxActiveDuration, 0.3f)));
            return;
        }

        StartCoroutine(MeleeAttackRoutine());
    }

    private IEnumerator AnimationEventAttackLockRoutine()
    {
        yield return MeleeAttackRoutine();
    }

    // Compatibilidade: se algum clip antigo ainda chamar este evento, a hitbox ainda abre corretamente.
    public void EnableMeleeHitboxFromAnimation()
    {
        if (ShouldSkipAI())
            return;

        BeginMeleeDamageWindow();
    }

    // Compatibilidade: fecha a janela de dano caso algum clip antigo ainda chame este evento.
    public void DisableMeleeHitboxFromAnimation()
    {
        EndMeleeDamageWindow();
    }

    // Compatibilidade com Animation Event antigo chamado "Punch".
    public void Punch()
    {
        EnableMeleeHitboxFromAnimation();
    }

    // Compatibilidade com Animation Event antigo chamado "EndPunch".
    public void EndPunch()
    {
        DisableMeleeHitboxFromAnimation();
    }


    // Abre a janela real de dano melee e limpa a lista de alvos já atingidos.
    private void BeginMeleeDamageWindow()
    {
        meleeDamageWindowActive = true;
        playerHitRegisteredThisWindow = false;
        meleeAttackWindowId++;
        damagedTargetsThisAttack.Clear();

        // CORREÇÃO (dano duplo): antes, o sistema legado (EnemyMeleeHitbox, que
        // aplica dano sozinho) e o sistema novo (EnemyAttackHitbox →
        // ProcessMeleeAttackHit) eram ativados JUNTOS. Prefab com os dois
        // configurados dava dano em dobro por soco. Agora o legado só é usado
        // quando NÃO há meleeAttackColliders configurados.
        bool hasModernColliders = meleeAttackColliders != null && meleeAttackColliders.Length > 0;

        if (meleeHitbox != null && !hasModernColliders)
            meleeHitbox.EnableHitbox(meleeDamage);

        if (hasModernColliders)
            SetMeleeAttackCollidersActive(true);

        if (debugMeleeHitbox)
            Debug.Log($"[{nameof(EnemyAIController)}] Objeto de hitbox melee ativado: {gameObject.name}");
    }

    // Fecha a janela de dano e desliga todos os colliders de ataque.
    private void EndMeleeDamageWindow()
    {
        meleeDamageWindowActive = false;
        playerHitRegisteredThisWindow = false;

        if (meleeHitbox != null)
            meleeHitbox.DisableHitbox();

        SetMeleeAttackCollidersActive(false);

        if (debugMeleeHitbox)
            Debug.Log($"[{nameof(EnemyAIController)}] Objeto de hitbox melee desativado: {gameObject.name}");
    }

    // Chamado pelos colliders filhos com EnemyAttackHitbox quando tocam algo durante o ataque.
    public void ProcessMeleeAttackHit(Collider other, EnemyAttackHitbox source)
    {
        if (!meleeDamageWindowActive || other == null || ShouldSkipAI())
            return;

        if (((1 << other.gameObject.layer) & meleeDamageMask.value) == 0)
            return;

        if (other.transform.IsChildOf(transform))
            return;

        PlayerControllerSystem playerController = other.GetComponentInParent<PlayerControllerSystem>();
        HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();

        bool isPlayerHit = playerController != null || other.CompareTag("Player");

        if (damagePlayerOnly && !isPlayerHit)
            return;

        if (onePlayerHitPerAttackWindow && isPlayerHit && playerHitRegisteredThisWindow)
            return;

        if (playerController != null && playerController.IsInvulnerable())
            return;

        Transform targetRoot = GetDamageTargetRoot(other, playerController, targetHealth);
        if (targetRoot != null && damagedTargetsThisAttack.Contains(targetRoot))
            return;

        Effect damageEffect = new Effect
        {
            effectType = Effect.EffectType.physical,
            power = meleeDamage
        };

        Effect[] effects = new Effect[] { damageEffect };
        bool sentDamage = TryApplyDamageToTarget(other, effects, meleeDamage);

        if (sentDamage)
        {
            if (targetRoot != null)
                damagedTargetsThisAttack.Add(targetRoot);

            if (isPlayerHit)
                playerHitRegisteredThisWindow = true;

            if (closeHitboxAfterSuccessfulHit && isPlayerHit)
                EndMeleeDamageWindow();
        }

        if (debugMeleeHitbox)
            Debug.Log($"[{nameof(EnemyAIController)}] Hit melee em {other.name}. Dano enviado: {sentDamage}");
    }


    // Tenta aplicar dano usando os nomes de método já comuns no projeto sem duplicar chamadas.
    private bool TryApplyDamageToTarget(Collider other, Effect[] effects, float rawDamage)
    {
        // CAMINHO TIPADO (#2): preferencial, sem reflection. EnemyHealth e
        // HealthSystem implementam IDamageable.
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            return damageable.ApplyEffect(effects);

        // FALLBACK (compatibilidade): reflection para alvos que ainda não
        // implementam IDamageable.
        Component[] components = other.GetComponentsInParent<Component>(true);

        if (TryInvokeMethod(components, "ApplyEffect", new object[] { effects }, typeof(Effect[])))
            return true;

        if (TryInvokeMethod(components, "TakeDamage", new object[] { rawDamage }, typeof(float)))
            return true;

        if (TryInvokeMethod(components, "ApplyDamage", new object[] { rawDamage }, typeof(float)))
            return true;

        if (TryInvokeMethod(components, "ReceiveDamage", new object[] { rawDamage }, typeof(float)))
            return true;

        return false;
    }

    // Invoca um método por reflection apenas se a assinatura existir exatamente.
    private bool TryInvokeMethod(Component[] components, string methodName, object[] args, System.Type argumentType)
    {
        if (components == null)
            return false;

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new System.Type[] { argumentType },
                null
            );

            if (method == null)
                continue;

            method.Invoke(component, args);
            return true;
        }

        return false;
    }

    // Define uma raiz estável para impedir múltiplos danos no mesmo ataque.
    private Transform GetDamageTargetRoot(Collider other, PlayerControllerSystem playerController, HealthSystem targetHealth)
    {
        if (playerController != null)
            return playerController.transform;

        if (targetHealth != null)
            return targetHealth.transform;

        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.transform;

        return other.transform.root;
    }

    // Liga/desliga as hitboxes melee. Por padrão, ativa/desativa o GameObject do collider.
    private void SetMeleeAttackCollidersActive(bool active)
    {
        if (meleeAttackColliders == null)
            return;

        for (int i = 0; i < meleeAttackColliders.Length; i++)
            SetSingleMeleeAttackObjectActive(meleeAttackColliders[i], active);
    }

    // Controla uma hitbox individual sem depender do Inspector durante a animação.
    private void SetSingleMeleeAttackObjectActive(Collider attackCollider, bool active)
    {
        if (attackCollider == null)
            return;

        GameObject hitboxObject = attackCollider.gameObject;

        if (activateMeleeHitboxObjects)
        {
            if (active)
            {
                if (!hitboxObject.activeSelf)
                    hitboxObject.SetActive(true);

                attackCollider.enabled = true;
            }
            else
            {
                attackCollider.enabled = false;

                if (hitboxObject.activeSelf)
                    hitboxObject.SetActive(false);
            }

            return;
        }

        attackCollider.enabled = active;
    }

    private IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(Mathf.Max(0f, attackImpactDelay));

        if (!ShouldSkipAI())
            BeginMeleeDamageWindow();

        yield return new WaitForSeconds(Mathf.Max(0f, hitboxActiveDuration));

        EndMeleeDamageWindow();

        isAttacking = false;
    }

    // Executa ataque ranged por tempo configurado no Inspector, sem depender de Animation Event.
    private IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;

        StopAgent();
        FaceTarget();

        string triggerToUse = !string.IsNullOrEmpty(rangedAttackTrigger) ? rangedAttackTrigger : attackTrigger;
        if (animator != null && !string.IsNullOrEmpty(triggerToUse))
            animator.SetTrigger(triggerToUse);

        float releaseDelay = Mathf.Max(0f, rangedProjectileReleaseDelay);
        if (releaseDelay > 0f)
            yield return new WaitForSeconds(releaseDelay);

        FaceTarget();
        FireRangedProjectile();

        float remainingLockTime = Mathf.Max(0f, rangedAttackLockDuration - releaseDelay);
        if (remainingLockTime > 0f)
            yield return new WaitForSeconds(remainingLockTime);

        isAttacking = false;
    }

    // Solta o projétil do inimigo ranged no tempo configurado pelo Inspector.
    private void FireRangedProjectile()
    {
        if (ShouldSkipAI())
            return;

        Spell spell;
        if (!TryGetRangedSpell(out spell))
            return;

        FaceTarget();

        switch (rangedProjectileFireMode)
        {
            case RangedProjectileFireMode.CannonFire:
                FireWithCannon(spell);
                break;

            case RangedProjectileFireMode.CannonThenDirectFallback:
                if (!FireWithCannon(spell))
                    SpawnProjectileDirect(spell);
                break;

            case RangedProjectileFireMode.DirectInstantiate:
            default:
                SpawnProjectileDirect(spell);
                break;
        }
    }

    // Valida o Spell antes do disparo. Spell é struct no ProjetoLumen, então não comparamos com null.
    private bool TryGetRangedSpell(out Spell spell)
    {
        spell = default;

        if (enemyAttacks == null || enemyAttacks.Length == 0)
        {
            if (showRangedSetupWarnings)
                Debug.LogWarning($"[{nameof(EnemyAIController)}] Enemy Attacks está vazio no inimigo ranged: {gameObject.name}.", this);
            return false;
        }

        if (recheckLineOfSightOnProjectileRelease && rangedNeedsLineOfSight && !HasDirectLineToTarget())
        {
            if (showRangedSetupWarnings)
                Debug.Log($"[{nameof(EnemyAIController)}] Disparo cancelado no release por falta de linha de visão: {gameObject.name}.", this);
            return false;
        }

        int attackIndex = randomizeRangedAttack ? Random.Range(0, enemyAttacks.Length) : 0;
        spell = enemyAttacks[Mathf.Clamp(attackIndex, 0, enemyAttacks.Length - 1)];

        if (spell.projectile == null)
        {
            if (showRangedSetupWarnings)
                Debug.LogWarning($"[{nameof(EnemyAIController)}] O Spell do Enemy Attacks não tem projectile configurado no inimigo: {gameObject.name}.", this);
            return false;
        }

        return true;
    }

    // Usa o Cannon existente quando o prefab já estiver configurado para ele.
    private bool FireWithCannon(Spell spell)
    {
        EnsureCannonReference();

        if (cannon == null)
        {
            if (showRangedSetupWarnings)
                Debug.LogWarning($"[{nameof(EnemyAIController)}] Cannon não encontrado em {gameObject.name}. Use Direct Instantiate ou configure um Cannon em filho/raiz.", this);
            return false;
        }

        cannon.Fire(spell);

        if (debugRangedAttack)
            Debug.Log($"[{nameof(EnemyAIController)}] Disparo ranged via Cannon: {cannon.name}.", this);

        return true;
    }

    // Instancia o prefab do Spell diretamente no ponto de disparo, evitando dependência de lógica interna do Cannon.
    private void SpawnProjectileDirect(Spell spell)
    {
        Transform spawnPoint = GetRangedProjectileSpawnPoint();
        Vector3 spawnPosition = spawnPoint.position + spawnPoint.forward * Mathf.Max(0f, projectileSpawnForwardOffset);
        Quaternion spawnRotation = GetProjectileAimRotation(spawnPosition, spawnPoint);

        UnityEngine.Object projectilePrefab = spell.projectile as UnityEngine.Object;
        if (projectilePrefab == null)
        {
            if (showRangedSetupWarnings)
                Debug.LogWarning($"[{nameof(EnemyAIController)}] Projectile do Spell não é um UnityEngine.Object válido em {gameObject.name}.", this);
            return;
        }

        UnityEngine.Object spawned = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        GameObject projectileObject = GetProjectileGameObject(spawned);

        if (projectileObject == null)
        {
            if (showRangedSetupWarnings)
                Debug.LogWarning($"[{nameof(EnemyAIController)}] Projétil foi instanciado, mas não foi possível obter o GameObject: {gameObject.name}.", this);
            return;
        }

        if (configureDirectProjectile)
            ConfigureSpawnedProjectile(projectileObject, spell, spawnRotation * Vector3.forward);

        if (debugRangedAttack)
            Debug.Log($"[{nameof(EnemyAIController)}] Projétil ranged instanciado diretamente: {projectileObject.name}.", this);
    }

    // Retorna o GameObject do objeto instanciado, seja prefab GameObject ou Component.
    private GameObject GetProjectileGameObject(UnityEngine.Object spawned)
    {
        if (spawned is GameObject go)
            return go;

        if (spawned is Component component)
            return component.gameObject;

        return null;
    }

    // Procura um ponto de disparo simples e previsível para o ranged.
    private Transform GetRangedProjectileSpawnPoint()
    {
        if (rangedProjectileSpawnPoint != null)
            return rangedProjectileSpawnPoint;

        if (autoFindRangedProjectileSpawnPoint)
            rangedProjectileSpawnPoint = FindRangedProjectileSpawnPointInChildren();

        if (rangedProjectileSpawnPoint != null)
            return rangedProjectileSpawnPoint;

        EnsureCannonReference();
        if (cannon != null)
            return cannon.transform;

        return transform;
    }

    // Busca nomes comuns de ponto de saída sem exigir um script novo.
    private Transform FindRangedProjectileSpawnPointInChildren()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        string[] preferredNames = { "firepoint", "fire_point", "muzzle", "projectilespawn", "projectile_spawn", "shootpoint", "shoot_point", "cannon" };

        for (int i = 0; i < preferredNames.Length; i++)
        {
            string key = preferredNames[i];
            for (int c = 0; c < children.Length; c++)
            {
                Transform child = children[c];
                if (child == null || child == transform)
                    continue;

                if (child.name.ToLowerInvariant().Contains(key))
                    return child;
            }
        }

        return null;
    }

    // Mira no player no instante do disparo.
    private Quaternion GetProjectileAimRotation(Vector3 spawnPosition, Transform fallbackForward)
    {
        Vector3 direction = playerTarget != null ? GetTargetFocusPosition() - spawnPosition : fallbackForward.forward;
        direction.y = Mathf.Abs(direction.y) < 0.01f ? 0f : direction.y;

        if (direction.sqrMagnitude < 0.0001f)
            direction = fallbackForward.forward;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    // Configura owner/target/velocidade quando o projétil foi criado diretamente. Usa reflection para não prender em um único script de projétil.
    private void ConfigureSpawnedProjectile(GameObject projectileObject, Spell spell, Vector3 direction)
    {
        TryConfigureProjectileOwner(projectileObject);
        TryConfigureProjectileTarget(projectileObject);
        TryCallProjectileInitializeMethods(projectileObject, spell);

        if (directProjectileLaunchSpeed > 0f)
        {
            Rigidbody rb = projectileObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = direction.normalized * directProjectileLaunchSpeed;
        }
    }

    // Tenta marcar o projétil como Enemy para evitar dano no próprio inimigo.
    private void TryConfigureProjectileOwner(GameObject projectileObject)
    {
        Component[] components = projectileObject.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            System.Type type = component.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int f = 0; f < fields.Length; f++)
            {
                FieldInfo field = fields[f];
                if (field.FieldType == typeof(ProjectileOwner))
                    field.SetValue(component, ProjectileOwner.Enemy);
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int p = 0; p < properties.Length; p++)
            {
                PropertyInfo property = properties[p];
                if (property.CanWrite && property.PropertyType == typeof(ProjectileOwner))
                    property.SetValue(component, ProjectileOwner.Enemy);
            }
        }
    }

    // Tenta passar o player como alvo se o projétil tiver campo/propriedade comum de target.
    private void TryConfigureProjectileTarget(GameObject projectileObject)
    {
        if (playerTarget == null)
            return;

        Component[] components = projectileObject.GetComponentsInChildren<Component>(true);
        string[] targetNames = { "target", "targetTransform", "playerTarget", "homingTarget" };

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            System.Type type = component.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int f = 0; f < fields.Length; f++)
            {
                FieldInfo field = fields[f];
                if (field.FieldType != typeof(Transform))
                    continue;

                if (System.Array.Exists(targetNames, n => string.Equals(n, field.Name, System.StringComparison.OrdinalIgnoreCase)))
                    field.SetValue(component, playerTarget);
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int p = 0; p < properties.Length; p++)
            {
                PropertyInfo property = properties[p];
                if (!property.CanWrite || property.PropertyType != typeof(Transform))
                    continue;

                if (System.Array.Exists(targetNames, n => string.Equals(n, property.Name, System.StringComparison.OrdinalIgnoreCase)))
                    property.SetValue(component, playerTarget);
            }
        }
    }

    // Chama métodos comuns de inicialização caso existam. Ignora silenciosamente se o projétil não precisar disso.
    private void TryCallProjectileInitializeMethods(GameObject projectileObject, Spell spell)
    {
        Component[] components = projectileObject.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            System.Type type = component.GetType();
            TryInvokeProjectileMethod(type, component, "SetOwner", new object[] { ProjectileOwner.Enemy }, new System.Type[] { typeof(ProjectileOwner) });
            TryInvokeProjectileMethod(type, component, "SetTarget", new object[] { playerTarget }, new System.Type[] { typeof(Transform) });
            TryInvokeProjectileMethod(type, component, "Initialize", new object[] { ProjectileOwner.Enemy }, new System.Type[] { typeof(ProjectileOwner) });
            TryInvokeProjectileMethod(type, component, "Initialize", new object[] { spell }, new System.Type[] { typeof(Spell) });
            TryInvokeProjectileMethod(type, component, "Initialize", new object[] { spell, ProjectileOwner.Enemy }, new System.Type[] { typeof(Spell), typeof(ProjectileOwner) });
            TryInvokeProjectileMethod(type, component, "Init", new object[] { spell, ProjectileOwner.Enemy }, new System.Type[] { typeof(Spell), typeof(ProjectileOwner) });
        }
    }

    private void TryInvokeProjectileMethod(System.Type type, Component component, string methodName, object[] args, System.Type[] signature)
    {
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, signature, null);
        if (method != null)
            method.Invoke(component, args);
    }

    // Garante que o Cannon possa estar em filhos, como mão, arma, cajado ou ponto de disparo.
    private void EnsureCannonReference()
    {
        if (cannon != null)
            return;

        cannon = GetComponentInChildren<Cannon>(true);
    }

    private IEnumerator AttackCooldownLock(float duration)
    {
        isAttacking = true;
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    private void MoveToLastSeenLocation()
    {
        ResumeAgent(chaseSpeed, 0.2f);
        SetDestinationThrottled(lastSeenLocation);
    }

    private void RetreatFromTarget()
    {
        if (playerTarget == null)
            return;

        Vector3 away = (transform.position - playerTarget.position).normalized;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = -transform.forward;

        Vector3 desired = transform.position + away.normalized * rangedRetreatDistance;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, rangedRetreatDistance + 1f, NavMesh.AllAreas))
        {
            ResumeAgent(chaseSpeed, 0.1f);
            SetDestinationThrottled(hit.position);
        }
    }

    #endregion

    #region Movement Helpers

    private void ResumeAgent(float speed, float stoppingDistance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void SetDestinationThrottled(Vector3 destination)
    {
        if (Time.time < nextRepathTime)
            return;

        nextRepathTime = Time.time + Mathf.Max(0.02f, repathInterval);
        SetDestinationSafe(destination);
    }

    private void SetDestinationSafe(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.SetDestination(destination);
    }

    private bool HasReachedDestination(float threshold)
    {
        if (agent == null || !agent.enabled || agent.pathPending)
            return false;

        if (float.IsInfinity(agent.remainingDistance))
            return false;

        return agent.remainingDistance <= Mathf.Max(threshold, agent.stoppingDistance);
    }

    private bool HasValidPathOrPending()
    {
        return agent != null && agent.enabled && (agent.pathPending || agent.hasPath);
    }

    private float GetStoppingDistanceForMode()
    {
        return combatMode == EnemyCombatMode.Ranged ? rangedIdealDistance : Mathf.Max(0.05f, attackRange - stopBuffer);
    }

    // Mantém o inimigo virado para a direção correta: alvo em combate, movimento na patrulha.
    private void UpdateFacingDirection()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (isAttacking && playerTarget != null)
        {
            FaceTarget();
            return;
        }

        if (currentStateID == EnemyStateID.Combat && keepFacingTargetInCombat && playerTarget != null)
        {
            FaceTarget();
            return;
        }

        if (!faceMovementDirection)
            return;

        Vector3 moveDirection = agent.desiredVelocity.sqrMagnitude > 0.01f ? agent.desiredVelocity : agent.velocity;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < faceMovementMinSpeed * faceMovementMinSpeed)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void FaceTarget()
    {
        if (!keepFacingTargetInCombat || playerTarget == null)
            return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    #endregion

    #region Animation

    private void UpdateAnimatorMovement()
    {
        if (animator == null || agent == null || agent.speed <= 0.01f)
            return;

        float speedFraction = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);

        if (hasSpeedParam)
            animator.SetFloat(speedHash, speedFraction, 0.1f, Time.deltaTime);

        if (hasAlertBoolParam)
            animator.SetBool(alertBoolHash, currentStateID == EnemyStateID.Alert || currentStateID == EnemyStateID.Combat);
    }

    #endregion

    #region External Events

    public void OnPurify()
    {
        // #1: isPurified agora é derivado de npcData.currentState — sem atribuição local.
        AbortCombat();
        StopAllCoroutines();

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsPurified", true);
            animator.SetBool("IsFallen", false);
        }

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(false);

        if (enemyHealth != null && enemyHealth.healthBarSlider != null)
            enemyHealth.healthBarSlider.gameObject.SetActive(false);

        gameObject.tag = "Untagged";
    }

    // Cancela ataques, hitboxes e movimento para nocaute/morte/purificação.
    public void AbortCombat()
    {
        StopAllCoroutines();
        isAttacking = false;

        EndMeleeDamageWindow();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    public void ResetAttackTimer()
    {
        attackTimer = attackCooldown;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, hearingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, combatMode == EnemyCombatMode.Ranged ? Mathf.Max(attackRange, rangedAttackRange) : attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, closeDetectionRadius);
    }

    #endregion
}
