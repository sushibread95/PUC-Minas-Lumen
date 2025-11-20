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
    }

    void Start()
    {
        agent.speed = walkSpeed;
        // Inicia a busca persistente pelo player
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
                Debug.Log($"IA ({gameObject.name}): Player ENCONTRADO via Coroutine!");
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        if (enemyHealth != null && enemyHealth.isFallen &&
            currentStateID != EnemyStateID.Fallen &&
            currentStateID != EnemyStateID.Dead)
        {
            ChangeState(EnemyStateID.Fallen);
        }

        currentState?.UpdateState();
    }

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

    // --- VERSÃO DE DEBUG DO CAN SEE TARGET ---
    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 targetDir = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        // 1. Checa Distância
        if (distToTarget > sightRange) 
        {
            // Debug.Log("IA DEBUG: Player longe demais."); 
            return false;
        }

        // 2. Checa Ângulo
        float dotProduct = Vector3.Dot(transform.forward, targetDir);
        if (dotProduct < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad)) 
        {
            // Debug.Log("IA DEBUG: Player fora do ângulo.");
            return false;
        }

        // 3. Checa Obstáculos
        // AQUI VEM O DIAGNÓSTICO
        if (Physics.Raycast(eyePos, targetDir, out RaycastHit hit, distToTarget, obstructionMask.value, QueryTriggerInteraction.Ignore))
        {
            // Se bater em algo, avisa o que é!
            // Se aparecer o nome do próprio Inimigo, é problema de Layer.
            Debug.Log($"<color=red>IA VISÃO BLOQUEADA POR: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})</color>");
            Debug.DrawLine(eyePos, hit.point, Color.red);
            return false;
        }

        // Se chegou aqui, está vendo!
        // Debug.Log("<color=green>IA VENDO O PLAYER!</color>");
        Debug.DrawLine(eyePos, targetPos, Color.green);
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
        Gizmos.color = Color.yellow;
        Quaternion rotLeft = Quaternion.Euler(0, -viewAngle / 2, 0);
        Quaternion rotRight = Quaternion.Euler(0, viewAngle / 2, 0);
        Vector3 lineLeft = rotLeft * transform.forward * sightRange;
        Vector3 lineRight = rotRight * transform.forward * sightRange;
        Gizmos.DrawLine(transform.position, transform.position + lineLeft);
        Gizmos.DrawLine(transform.position, transform.position + lineRight);
    }
}