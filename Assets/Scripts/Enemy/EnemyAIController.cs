// EnemyAIController.cs
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem; // Para o debug com 'F'

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(CorruptedNPC))]
[RequireComponent(typeof(LockOnTarget))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public HealthSystem healthSystem;
    [HideInInspector] public CorruptedNPC npcData;
    [HideInInspector] public LockOnTarget lockOnTarget;
    public Cannon cannon;
    [Tooltip("Distância para a I.A. decidir ATACAR (se já estiver em Alerta).")]
    public float combatAggroRange = 7f;
    [Tooltip("Tempo (segundos) que a I.A. continua procurando após perder o player de vista em combate.")]
    public float combatMemoryDuration = 3.0f;
    [Header("Combat Settings")]
    public Spell[] enemyAttacks;
    public float attackRange = 10f;
    public float attackCooldown = 2.0f;

    [Header("Sensing Atributes")]
    public float sightRange = 15f;
    public float hearingRange = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    // --- FSM Runtime ---
    private IEnemyState currentState;
    [HideInInspector] public EnemyStateID currentStateID = EnemyStateID.Idle;

    // --- Cache para o Player ---
    [HideInInspector] public Transform playerTarget;
    [HideInInspector] public Vector3 lastSeenLocation;

    [Header("Patrol Settings")]
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.0f;
    public Transform[] waypoints;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        healthSystem = GetComponent<HealthSystem>();
        npcData = GetComponent<CorruptedNPC>();
        lockOnTarget = GetComponent<LockOnTarget>();
        cannon = GetComponent<Cannon>();

        if (GameObject.FindGameObjectWithTag("Player") is GameObject playerGO)
        {
            playerTarget = playerGO.transform;
            Debug.Log("<color=green>IA: Alvo 'Player' encontrado com sucesso!</color>");
        }
        else
        {
            Debug.LogError("IA: ERRO CRÍTICO! Não foi possível encontrar o 'Player'. A IA não vai funcionar. Verifique a Tag 'Player'.");
        }

        agent.speed = walkSpeed;
        ChangeState(EnemyStateID.Patrol);
    }

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (currentStateID == EnemyStateID.Patrol || currentStateID == EnemyStateID.Alert)
            {
                ChangeState(EnemyStateID.Combat);
            }
            else
            {
                ChangeState(EnemyStateID.Patrol);
            }
        }

        currentState?.UpdateState();

        // Transições Globais
        if (healthSystem.isDead && currentStateID != EnemyStateID.Dead)
        {
            ChangeState(EnemyStateID.Dead);
        }
        else if (npcData.currentState == NPCState.Nocauteado && currentStateID != EnemyStateID.KnockedOut)
        {
            ChangeState(EnemyStateID.KnockedOut);
        }
    }

    void FixedUpdate()
    {
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
            EnemyStateID.KnockedOut => new KnockedOutState(this),
            EnemyStateID.Dead => new DeadState(this),
            _ => null,
        };
    }

    // --- FUNÇÕES DE SENSOR (VISÃO) ---
    // --- FUNÇÕES DE SENSOR (VISÃO) ---
    public bool CanSeeTarget()
    {
        if (playerTarget == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
        Vector3 targetDir = (targetPos - eyePos).normalized;
        float distToTarget = Vector3.Distance(eyePos, targetPos);

        if (distToTarget > sightRange)
        {
            return false;
        }

        float dotProduct = Vector3.Dot(transform.forward, targetDir);
        if (dotProduct < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad))
        {
            return false;
        }

        if (Physics.Raycast(
                eyePos,
                targetDir,
                out RaycastHit hit,
                distToTarget,
                obstructionMask.value,
                QueryTriggerInteraction.Ignore
            ))
        {

            return false;
        }

        lastSeenLocation = playerTarget.position;
        return true;
    }

    public bool CanHearTarget()
    {
        if (playerTarget == null) return false;

        if (playerTarget.GetComponent<PlayerControllerSystem>() is PlayerControllerSystem playerController)
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