using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CorruptedNPC))]
[RequireComponent(typeof(LockOnTarget))]
[RequireComponent(typeof(Cannon))] // --- RESTAURADO: Exige o Canhão ---
public class EnemyShooterController : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyHealth enemyHealth;
    [HideInInspector] public CorruptedNPC npcData;
    [HideInInspector] public LockOnTarget lockOnTarget;
    private Cannon cannon; // --- RESTAURADO ---
    
    [HideInInspector] public bool isPurified = false;
    
    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed"; 

    [Header("Combat Settings (Shooter)")]
    public Spell[] enemyAttacks; // --- RESTAURADO: Lista de projéteis ---
    public float combatAggroRange = 15f; // Aumentado para Shooter (era 7)
    public float attackRange = 10f;      // Aumentado para atirar de longe (era 1.5)
    public float attackCooldown = 2.0f;
    public string attackTrigger = "Attack";

    [Header("Sensing Attributes")] // Mantido idêntico ao Brawler
    public float sightRange = 15f;
    public float hearingRange = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("Patrol Settings")] // Mantido idêntico ao Brawler
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.0f;
    public Transform[] waypoints;

    // --- ESTADO INTERNO ---
    private Transform playerTarget; 
    private Vector3 lastSeenLocation;
    private float attackTimer;
    private int currentWaypointIndex = 0;
    private bool isAlert = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        npcData = GetComponent<CorruptedNPC>();
        lockOnTarget = GetComponent<LockOnTarget>();
        cannon = GetComponent<Cannon>(); // Pega a referência do Canhão
        
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        // Segurança do NavMesh
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    void Start()
    {
        agent.speed = walkSpeed;
        StartCoroutine(FindPlayerRoutine());
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
        if (enemyHealth.isDead || enemyHealth.isFallen) return;

        UpdateAnimatorMovement();

        if (playerTarget == null) return;

        // --- LÓGICA DE COMPORTAMENTO (Idêntica à Máquina de Estados) ---
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSee = CanSeeTarget();

        // 1. COMBATE (Se viu ou está perto/alerta)
        if (canSee || (isAlert && distanceToPlayer <= combatAggroRange))
        {
            isAlert = true;
            lastSeenLocation = playerTarget.position;
            EngageCombat(distanceToPlayer);
        }
        // 2. BUSCA (Se perdeu de vista mas estava alerta)
        else if (isAlert)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(lastSeenLocation);
            
            if (agent.remainingDistance < 1f)
            {
                isAlert = false; // Desiste e volta a patrulhar
            }
        }
        // 3. PATRULHA (Padrão)
        else
        {
            PatrolLogic();
        }
    }

    // --- LÓGICA DE TIRO (AQUI ESTÁ A MUDANÇA) ---
    private void EngageCombat(float distance)
    {
        agent.speed = chaseSpeed;
        attackTimer += Time.deltaTime;

        // Está no alcance de TIRO?
        if (distance <= attackRange)
        {
            agent.isStopped = true; // Para de correr para atirar
            
            // Gira para o player
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            }

            // Dispara
            if (attackTimer >= attackCooldown)
            {
                PerformRangedAttack();
                attackTimer = 0f;
            }
        }
        else
        {
            // Corre atrás para entrar no alcance
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
    }

    public void PerformRangedAttack()
    {
        if (animator != null) animator.SetTrigger(attackTrigger);

        // --- LÓGICA DO CANHÃO RESTAURADA ---
        if (cannon != null && enemyAttacks != null && enemyAttacks.Length > 0)
        {
            // Dispara o primeiro ataque da lista (projétil configurado)
            cannon.Fire(enemyAttacks[0]);
        }
    }

    private void PatrolLogic()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.speed = walkSpeed;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        else if (agent.destination != waypoints[currentWaypointIndex].position)
        {
             agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    // --- AUXILIARES (Idênticos ao Original) ---

    void UpdateAnimatorMovement()
    {
        if (animator == null || agent == null) return;
        float speedFraction = agent.velocity.magnitude / agent.speed;
        animator.SetFloat(speedParam, speedFraction, 0.1f, Time.deltaTime);
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
            animator.SetBool("IsFallen", false); 
        }

        if (lockOnTarget != null) lockOnTarget.enabled = false;
        
        if (enemyHealth != null && enemyHealth.healthBarSlider != null)
            enemyHealth.healthBarSlider.gameObject.SetActive(false);

        gameObject.tag = "Untagged";
    }

    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 targetDir = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        // Ajuste de visibilidade baseado no PlayerStats
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // Visualiza o alcance do tiro
    }
}