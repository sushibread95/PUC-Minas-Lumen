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
    [HideInInspector] public bool isPurified = false;
    
    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed"; 

    [Header("Combat Settings (IA)")]
    public float combatAggroRange = 7f;
    public float combatMemoryDuration = 3.0f;
    public float attackRange = 1.5f; 
    public float attackCooldown = 2.0f;

    [Header("Melee Settings (Timing)")]
    public EnemyMeleeHitbox meleeHitbox;
    public string attackTrigger = "Attack";
    public float meleeDamage = 15f;
    
    [Tooltip("Quanto tempo esperar APÓS iniciar a animação para ligar o dano (Windup)")]
    public float attackImpactDelay = 0.4f; 
    
    [Tooltip("Por quanto tempo a hitbox fica ligada")]
    public float hitboxActiveDuration = 0.2f; 

    [Header("Fallen State")]
    public float fallenDuration = 15f;

    [Header("Combat Refs")]
    public EnemyGrabber grabberComponent;

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
    public EnemyStateID currentStateID = EnemyStateID.Idle;
    [HideInInspector] public Transform playerTarget; 
    [HideInInspector] public Vector3 lastSeenLocation;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        npcData = GetComponent<CorruptedNPC>();
        lockOnTarget = GetComponent<LockOnTarget>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        agent.speed = walkSpeed;
        StartCoroutine(FindPlayerRoutine());

        if (waypoints == null || waypoints.Length == 0) ChangeState(EnemyStateID.Idle); 
        else ChangeState(EnemyStateID.Patrol);
    }

    IEnumerator FindPlayerRoutine()
    {
        while (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
            else yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (isPurified) return;

        UpdateAnimatorMovement();

        if (playerTarget == null) return;

        if (enemyHealth != null && enemyHealth.isFallen &&
            currentStateID != EnemyStateID.Fallen &&
            currentStateID != EnemyStateID.Dead)
        {
            ChangeState(EnemyStateID.Fallen);
        }

        currentState?.UpdateState();
    }

    void UpdateAnimatorMovement()
    {
        if (animator == null || agent == null) return;
        float speedFraction = agent.velocity.magnitude / agent.speed;
        animator.SetFloat(speedParam, speedFraction, 0.1f, Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (isPurified) return;
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
            EnemyStateID.Fallen => new FallenState(this), // Agora funciona!
            EnemyStateID.Dead => new DeadState(this),
            _ => null,
        };
    }

        public void PerformMeleeAttack()
    {
        if (animator != null)
        {
            // Toca a animação (GrabAttempt ou Punch)
            animator.SetTrigger(attackTrigger);

            // DECISÃO: É um Grabber ou um Pugilista?
            
            // Se tiver o componente Grabber configurado, usa a lógica de Grab
            if (grabberComponent != null)
            {
                grabberComponent.StartGrabAttempt();
            }
            // Se não, usa a lógica de Soco (MeleeHitbox)
            else 
            {
                StartCoroutine(MeleeAttackRoutine());
            }
        }
    }
    
        private IEnumerator MeleeAttackRoutine()
    {
        yield return new WaitForSeconds(attackImpactDelay);

        if (meleeHitbox != null) 
        {
            meleeHitbox.EnableHitbox(meleeDamage);
        }

        yield return new WaitForSeconds(hitboxActiveDuration);

        if (meleeHitbox != null) 
        {
            meleeHitbox.DisableHitbox();
        }
    }

    public void OnPurify()
    {
        isPurified = true;
        StopAllCoroutines(); 

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false; 
        }

        if (animator != null)
        {
            animator.SetBool("IsPurified", true);
            animator.SetBool("IsFallen", false); 
        }

        if (lockOnTarget != null) lockOnTarget.enabled = false;
        
        if (enemyHealth != null && enemyHealth.healthBarSlider != null)
            enemyHealth.healthBarSlider.gameObject.SetActive(false);

        gameObject.tag = "Untagged";
        
        if (meleeHitbox != null) meleeHitbox.DisableHitbox();
    }

    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 targetDir = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        float currentSightRange = sightRange; 
        var pStats = playerTarget.GetComponent<PlayerControllerSystem>();
        if (pStats != null) currentSightRange *= pStats.visibilityFactor;

        if (distToTarget > currentSightRange) return false;

        float dotProduct = Vector3.Dot(transform.forward, targetDir);
        if (dotProduct < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad)) return false;

        if (Physics.Raycast(eyePos, targetDir, out RaycastHit hit, distToTarget, obstructionMask)) return false;

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