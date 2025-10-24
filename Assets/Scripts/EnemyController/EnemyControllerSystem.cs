using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyControllerSystem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthSystem health;
    [SerializeField] private Animator animator;
    [SerializeField] private Cannon cannon;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    [Header("Patrol")]
    [SerializeField] private Transform[] walkPositions;
    private int index = 0;
    [SerializeField] private Vector3 walkPoint;
    [SerializeField] private bool walkPointSet;
    [SerializeField] private float walkPointRange;
    [SerializeField] private float walkPointDistanceCheck = 0.2f;
    [Header("Attack")]
    [SerializeField] private Spell[] spells;
    [SerializeField] private bool hasAttacked;
    [Header("Other States")]
    [SerializeField] private float playerDistanceCheck;
    [SerializeField] private float sightRange, attackRange, audioRange;
    [SerializeField] private bool inSight, inRange, heard;
    
    private bool isTooClose;
    //[SerializeField] private bool idle, alerted, combat;
    [Header("Animation Params (names in Animator)")]
    [Tooltip("Float usado no BlendTree Locomotion (0..1).")]
    public string speedParam = "Speed";
    [Tooltip("Bool que marca se está no chão (opcional).")]
    public string groundedParam = "Grounded";
    [Tooltip("Bool que marca se está agachado (usada em BlendTrees/Layer de Crouch)")]
    public string crouchBoolParam = "IsCrouching";
    [Tooltip("Locomotion BlendTree param X (strafing)")]
    public string moveXParam = "MoveX";
    [Tooltip("Locomotion BlendTree param Y (forward)")]
    public string moveYParam = "MoveY";
    private int speedHash, groundedHash, crouchHash, moveXHash, moveYHash;
    private bool isGrounded;
    private Coroutine spellRoutine;
    private float moveLockUntil = 0f;
    private float nextSpellTime = 0f;
    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>();
        
        if (!string.IsNullOrEmpty(speedParam))      speedHash = Animator.StringToHash(speedParam);
        if (!string.IsNullOrEmpty(groundedParam))   groundedHash = Animator.StringToHash(groundedParam);
        if (!string.IsNullOrEmpty(crouchBoolParam)) crouchHash = Animator.StringToHash(crouchBoolParam);
        if (!string.IsNullOrEmpty(moveXParam))      moveXHash = Animator.StringToHash(moveXParam);
        if (!string.IsNullOrEmpty(moveYParam)) moveYHash = Animator.StringToHash(moveYParam);
        if (animator)
        {
            if (groundedHash != 0) animator.SetBool(groundedHash, isGrounded);
        }
    }
    private void Update()
    {
        inSight = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        inRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        if (!inSight)
        {
            if (!heard) Patrol();
            else Alert();
        }
        else
        {
            Chase();
            if (inRange && !hasAttacked) Attack();
        }
        
        if (animator)
        {
            if (groundedHash != 0) animator.SetBool(groundedHash, isGrounded);
        }
    }
    private void SearchWalkPoint()
    {
        if (!walkPointSet)
        {
            if (walkPositions.Length == 0)
            {
                float randomZ = Random.Range(-walkPointRange, walkPointRange);
                float randomX = Random.Range(-walkPointRange, walkPointRange);
                walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
            }
            else
            {
                index++;
                if (index > walkPositions.Length - 1) index = 0;
                walkPoint = walkPositions[index].position;
            }
            if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)) walkPointSet = true;
        }
    }
    private void Patrol()
    {
        if (!walkPointSet) SearchWalkPoint();
        else agent.SetDestination(walkPoint);
        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        if (distanceToWalkPoint.magnitude < walkPointDistanceCheck) walkPointSet = false;
    }
    private void Chase()
    {
        Vector3 distanceToPlayer = player.position - transform.position;
        Debug.Log(distanceToPlayer.magnitude);
        agent.SetDestination(player.position);
        if (distanceToPlayer.magnitude <= playerDistanceCheck && distanceToPlayer.magnitude >= 1f) agent.SetDestination(this.transform.position);
        else if (distanceToPlayer.magnitude < 1f) agent.SetDestination(this.transform.position - distanceToPlayer);
    }
    private void Attack()
    {
        hasAttacked = true;
        agent.SetDestination(transform.position);
        if (health.CheckEffect(spells[0].castEffects))
        {
            Vector3 offset = new Vector3(0f, 1.5f, 0f);
            cannon.transform.LookAt(player.position + offset);
            spellRoutine = StartCoroutine(CastSpellRoutine(0));
        }  
        Invoke("ResetAttack", spells[0].fireRate);
    }
    private void ResetAttack() => hasAttacked = false;
    private IEnumerator CastSpellRoutine(int index)
    {
        // guards
        if (spells == null || index < 0 || index >= spells.Length)
            yield break;

        var spell = spells[index];

        // Cooldown gate
        if (Time.time < nextSpellTime)
            yield break;

        // Lock movement for the cast window
        if (spell.castLockTime > 0f)
            moveLockUntil = Mathf.Max(moveLockUntil, Time.time + spell.castLockTime);

        // Trigger animation
        if (animator && !string.IsNullOrEmpty(spell.spellTriggerParam))
            animator.SetTrigger(spell.spellTriggerParam);

        // Wait a frame to allow transition
        yield return null;

        // Measure state length (current or next)
        float measuredLen = 0.3f; // fallback
        float timeout = 0.5f;
        while (timeout > 0f)
        {
            var st = animator ? animator.GetCurrentAnimatorStateInfo(spell.spellLayerIndex) : default;
            if (animator && (st.IsName(spell.spellStateName) || animator.GetNextAnimatorStateInfo(spell.spellLayerIndex).IsName(spell.spellStateName)))
            {
                // small settle frame
                yield return null;
                st = animator.GetCurrentAnimatorStateInfo(spell.spellLayerIndex);
                if (st.length > 0.01f) measuredLen = st.length;
                break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Fire moment = 20% of measured state
        float fireDelay = measuredLen * 0.2f;
        if (fireDelay > 0f) yield return new WaitForSeconds(fireDelay);

        // FIRE!
        if (cannon != null)
            cannon.Fire(spell);
        else
            Debug.LogWarning("PlayerControllerSystem: Cannon não encontrado para disparar o spell.");

        // Set cooldown based on measuredLen (keeps previous behavior)
        nextSpellTime = Time.time + measuredLen;

        // Optional: wait rest of animation (to avoid immediate re-trigger visuals)
        yield return new WaitForSeconds(measuredLen * 0.8f);

        if (animator && !string.IsNullOrEmpty(spell.spellTriggerParam))
            animator.ResetTrigger(spell.spellTriggerParam);

        spellRoutine = null;
    }
    private void Alert()
    {
        //stop for a second and go inspect  a point
        Search();
    }
    private void Search()
    {
        //go to the source of the audio heard from the npc
    }
    private void Flee()
    {

    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(this.transform.position, sightRange);
    }
}
