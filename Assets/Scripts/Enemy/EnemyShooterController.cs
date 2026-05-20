using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyShooterFireMode
{
    DirectInstantiate,
    CannonFire
}

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CorruptedNPC))]
[RequireComponent(typeof(LockOnTarget))]
public class EnemyShooterController : MonoBehaviour
{
    #region Inspector - Compatibility

    [Header("AVISO DE COMPATIBILIDADE")]
    [Tooltip("Este script foi mantido para prefabs antigos. Para inimigos novos, prefira EnemyAIController com Tipo de Inimigo = Ranged.")]
    [SerializeField] private bool disableIfUnifiedAIExists = true;

    #endregion

    #region Inspector - Components

    [Header("REFERÊNCIAS")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyHealth enemyHealth;
    [HideInInspector] public CorruptedNPC npcData;
    [HideInInspector] public LockOnTarget lockOnTarget;
    [SerializeField] private Cannon cannon;
    [SerializeField] private EnemyShooterFireMode fireMode = EnemyShooterFireMode.DirectInstantiate;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpawnForwardOffset = 0.15f;
    [SerializeField] private float directProjectileLaunchSpeed = 0f;
    [HideInInspector] public bool isPurified = false;

    [Header("ANIMAÇÃO")]
    public Animator animator;
    public string speedParam = "Speed";

    #endregion

    #region Inspector - Combat

    [Header("COMBATE RANGED")]
    public Spell[] enemyAttacks;
    public float combatAggroRange = 15f;
    public float attackRange = 10f;
    [SerializeField] private float idealDistance = 7f;
    [SerializeField] private float minDistance = 4f;
    public float attackCooldown = 2.0f;
    public string attackTrigger = "Attack";
    [SerializeField] private float fireDelay = 0.15f;

    [Header("PERCEPÇÃO")]
    public float sightRange = 15f;
    public float hearingRange = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("PATRULHA")]
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.0f;
    public Transform[] waypoints;

    #endregion

    #region Runtime

    private Transform playerTarget;
    private Vector3 lastSeenLocation;
    private float attackTimer;
    private int currentWaypointIndex;
    private bool isAlert;
    private bool isAttacking;
    private float nextRepathTime;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        npcData = GetComponent<CorruptedNPC>();
        lockOnTarget = GetComponent<LockOnTarget>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>(true);
        if (animator == null) animator = GetComponentInChildren<Animator>();

        EnemyAIController unifiedAI = GetComponent<EnemyAIController>();
        if (disableIfUnifiedAIExists && unifiedAI != null && unifiedAI.enabled)
        {
            Debug.LogWarning($"{name}: EnemyShooterController desativado porque EnemyAIController já existe. Use EnemyAIController em modo Ranged.", this);
            enabled = false;
            return;
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.stoppingDistance = idealDistance;
        }
    }

    private void Start()
    {
        if (agent != null) agent.speed = walkSpeed;
        StartCoroutine(FindPlayerRoutine());
    }

    private void Update()
    {
        if (isPurified || enemyHealth == null || enemyHealth.isDead || enemyHealth.isFallen || playerTarget == null)
            return;

        UpdateAnimatorMovement();

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        bool canSee = CanSeeTarget();

        if (canSee || (isAlert && distance <= combatAggroRange))
        {
            isAlert = true;
            lastSeenLocation = playerTarget.position;
            EngageCombat(distance, canSee);
        }
        else if (isAlert)
        {
            MoveToLastSeenLocation();
        }
        else
        {
            PatrolLogic();
        }
    }

    #endregion

    #region Setup

    private IEnumerator FindPlayerRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (playerTarget == null && enabled)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
            else yield return wait;
        }
    }

    #endregion

    #region Combat

    private void EngageCombat(float distance, bool canSee)
    {
        attackTimer += Time.deltaTime;

        if (distance < minDistance)
        {
            RetreatFromPlayer();
            FacePlayer();
            return;
        }

        if (distance <= attackRange && canSee)
        {
            StopAgent();
            FacePlayer();

            if (attackTimer >= attackCooldown && !isAttacking)
            {
                StartCoroutine(RangedAttackRoutine());
                attackTimer = 0f;
            }

            return;
        }

        ResumeAgent(chaseSpeed, idealDistance);
        SetDestinationThrottled(playerTarget.position);
        FacePlayer();
    }

    private IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (fireDelay > 0f)
            yield return new WaitForSeconds(fireDelay);

        FireProjectile();

        isAttacking = false;
    }

    private void FireProjectile()
    {
        if (enemyAttacks == null || enemyAttacks.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemyShooterController)}] Enemy Attacks vazio em {gameObject.name}.", this);
            return;
        }

        Spell spell = enemyAttacks[0];
        if (spell.projectile == null)
        {
            Debug.LogWarning($"[{nameof(EnemyShooterController)}] Spell sem projectile em {gameObject.name}.", this);
            return;
        }

        if (fireMode == EnemyShooterFireMode.CannonFire)
        {
            if (cannon == null) cannon = GetComponentInChildren<Cannon>(true);
            if (cannon != null)
            {
                cannon.Fire(spell);
                return;
            }
        }

        SpawnProjectileDirect(spell);
    }

    private void SpawnProjectileDirect(Spell spell)
    {
        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : (cannon != null ? cannon.transform : transform);
        Vector3 direction = playerTarget != null ? (playerTarget.position + Vector3.up) - spawn.position : spawn.forward;
        if (direction.sqrMagnitude < 0.0001f) direction = spawn.forward;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Vector3 position = spawn.position + spawn.forward * Mathf.Max(0f, projectileSpawnForwardOffset);

        UnityEngine.Object prefab = spell.projectile as UnityEngine.Object;
        if (prefab == null)
            return;

        UnityEngine.Object spawned = Instantiate(prefab, position, rotation);
        GameObject go = spawned is GameObject g ? g : (spawned is Component c ? c.gameObject : null);
        if (go == null) return;

        TryConfigureProjectileOwner(go);

        if (directProjectileLaunchSpeed > 0f)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = direction.normalized * directProjectileLaunchSpeed;
        }
    }

    private void TryConfigureProjectileOwner(GameObject projectileObject)
    {
        Component[] components = projectileObject.GetComponentsInChildren<Component>(true);
        foreach (Component component in components)
        {
            if (component == null) continue;
            System.Type type = component.GetType();
            MethodInfo method = type.GetMethod("SetOwner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(ProjectileOwner) }, null);
            if (method != null) method.Invoke(component, new object[] { ProjectileOwner.Enemy });
        }
    }

    private void RetreatFromPlayer()
    {
        Vector3 away = (transform.position - playerTarget.position).normalized;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) away = -transform.forward;

        Vector3 desired = transform.position + away * 3f;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            ResumeAgent(chaseSpeed, 0.1f);
            SetDestinationThrottled(hit.position);
        }
    }

    #endregion

    #region Patrol And Search

    private void PatrolLogic()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            StopAgent();
            return;
        }

        ResumeAgent(walkSpeed, 0.5f);

        if (!agent.hasPath && !agent.pathPending)
            agent.SetDestination(waypoints[currentWaypointIndex].position);

        if (!agent.pathPending && !float.IsInfinity(agent.remainingDistance) && agent.remainingDistance <= 0.8f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void MoveToLastSeenLocation()
    {
        ResumeAgent(chaseSpeed, 0.2f);
        SetDestinationThrottled(lastSeenLocation);

        if (!agent.pathPending && !float.IsInfinity(agent.remainingDistance) && agent.remainingDistance <= 1f)
            isAlert = false;
    }

    #endregion

    #region Perception

    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 toTarget = targetPos - eyePos;
        float distance = toTarget.magnitude;

        float currentSightRange = sightRange;
        PlayerControllerSystem pStats = playerTarget.GetComponent<PlayerControllerSystem>();
        if (pStats != null) currentSightRange *= Mathf.Max(0.15f, pStats.visibilityFactor);

        if (distance > currentSightRange) return false;

        Vector3 direction = toTarget.normalized;
        float minDot = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        if (Vector3.Dot(transform.forward, direction) < minDot && distance > 2.25f) return false;

        if (obstructionMask.value != 0 && Physics.Linecast(eyePos, targetPos, out RaycastHit hit, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(playerTarget))
                return false;
        }

        lastSeenLocation = playerTarget.position;
        return true;
    }

    #endregion

    #region Movement Helpers

    private void ResumeAgent(float speed, float stoppingDistance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void SetDestinationThrottled(Vector3 destination)
    {
        if (Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + 0.18f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(destination);
    }

    private void FacePlayer()
    {
        if (playerTarget == null) return;
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
    }

    #endregion

    #region Animation And Purify

    private void UpdateAnimatorMovement()
    {
        if (animator == null || agent == null || agent.speed <= 0.01f) return;
        float speedFraction = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
        animator.SetFloat(speedParam, speedFraction, 0.1f, Time.deltaTime);
    }

    public void OnPurify()
    {
        isPurified = true;
        StopAllCoroutines();
        StopAgent();
        if (agent != null) agent.enabled = false;
        if (animator != null) animator.SetBool("IsFallen", false);
        if (lockOnTarget != null) lockOnTarget.SetTargetable(false);
        if (enemyHealth != null && enemyHealth.healthBarSlider != null)
            enemyHealth.healthBarSlider.gameObject.SetActive(false);
        gameObject.tag = "Untagged";
    }

    #endregion
}
