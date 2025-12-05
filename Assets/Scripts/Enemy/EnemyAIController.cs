using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CorruptedNPC))]
[RequireComponent(typeof(LockOnTarget))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyHealth enemyHealth;
    [HideInInspector] public CorruptedNPC npcData;
    [HideInInspector] public LockOnTarget lockOnTarget;
    public Cannon cannon;
    
    [Header("Animation")] // --- ADIÇÃO ---
    public Animator animator;
    public string speedParam = "Speed"; // Blend Tree (0 a 1)
    // --------------------

    [Header("Combat Settings")]
    public float combatAggroRange = 7f;
    public float combatMemoryDuration = 3.0f;
    public Spell[] enemyAttacks;
    public float attackRange = 10f;
    public float attackCooldown = 2.0f;

    [Header("Fallen State")]
    public float fallenDuration = 15f;

    [Header("Sensing Attributes")]
    public float sightRange = 15f;
    public float hearingRange = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("Patrol Settings")]
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.0f;
    public Transform[] waypoints;

    // --- ESTADO ---
    private IEnemyState currentState;
    [HideInInspector] public EnemyStateID currentStateID = EnemyStateID.Idle;
    [HideInInspector] public Transform playerTarget; 
    [HideInInspector] public Vector3 lastSeenLocation;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        npcData = GetComponent<CorruptedNPC>();
        lockOnTarget = GetComponent<LockOnTarget>();
        cannon = GetComponent<Cannon>();
        
        // Auto-assign animator
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        agent.speed = walkSpeed;
        StartCoroutine(FindPlayerRoutine());
        ChangeState(EnemyStateID.Patrol);
    }

    IEnumerator FindPlayerRoutine()
    {
        while (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTarget = p.transform;
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    void Update()
    {
        // --- ADIÇÃO: Atualiza animação de movimento ---
        UpdateAnimatorMovement();
        // ----------------------------------------------

        if (playerTarget == null) return;

        if (enemyHealth != null && enemyHealth.isFallen &&
            currentStateID != EnemyStateID.Fallen &&
            currentStateID != EnemyStateID.Dead)
        {
            ChangeState(EnemyStateID.Fallen);
        }

        currentState?.UpdateState();
    }

    // --- FUNÇÃO ADICIONADA: Sincroniza NavMesh com Animator ---
    void UpdateAnimatorMovement()
    {
        if (animator == null || agent == null) return;

        // Pega a velocidade normalizada (0 a 1) baseada na velocidade máxima do agente
        float speedFraction = agent.velocity.magnitude / agent.speed;
        
        // Se estiver perseguindo (velocidade alta), o fraction será 1. Se patrulhando, será menor.
        // Dica: Use DampTime para suavizar a transição
        animator.SetFloat(speedParam, speedFraction, 0.1f, Time.deltaTime);
    }
    // -----------------------------------------------------------

    void FixedUpdate()
    {
        if (playerTarget == null) return;
        currentState?.FixedUpdateState();
    }

    public void ChangeState(EnemyStateID newStateID)
    {
        if (currentStateID == newStateID) return;
        currentState?.ExitState();
        currentState = GetStateInstance(newStateID);
        currentStateID = newStateID;
        currentState?.EnterState();
    }

    private IEnemyState GetStateInstance(EnemyStateID stateID)
    {
        return stateID switch
        {
            EnemyStateID.Patrol => new PatrolState(this),
            EnemyStateID.Alert => new AlertState(this),
            EnemyStateID.Combat => new CombatState(this),
            EnemyStateID.Fallen => new FallenState(this),
            EnemyStateID.Dead => new DeadState(this),
            _ => null,
        };
    }

    // ... (Mantive o resto das funções de CanSeeTarget/CanHearTarget iguais) ...
    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 targetDir = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        float currentSightRange = sightRange; 
        var pStats = playerTarget.GetComponent<PlayerControllerSystem>();
        if (pStats != null)
        {
            currentSightRange *= pStats.visibilityFactor;
        }

        if (distToTarget > currentSightRange) return false;

        float dotProduct = Vector3.Dot(transform.forward, targetDir);
        if (dotProduct < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad)) return false;

        if (Physics.Raycast(eyePos, targetDir, out RaycastHit hit, distToTarget, obstructionMask))
        {
            return false;
        }

        lastSeenLocation = playerTarget.position;
        return true;
    }

    public bool CanHearTarget()
    {
        if (playerTarget == null) return false;
        var playerController = playerTarget.GetComponent<PlayerControllerSystem>();
        if (playerController != null)
        {
            float noiseMultiplier = playerController.noiseLevel;
            float effectiveHearingRange = hearingRange * noiseMultiplier;

            if (effectiveHearingRange < 0.1f) return false;

            float distToTarget = Vector3.Distance(transform.position, playerTarget.position);
            if (distToTarget < effectiveHearingRange)
            {
                lastSeenLocation = playerTarget.position;
                return true;
            }
        }
        return false;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}