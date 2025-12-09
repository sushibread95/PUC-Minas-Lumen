using UnityEngine;
using UnityEngine.AI;

public class CombatState : IEnemyState
{
    private readonly EnemyAIController controller;
    private float attackTimer;
    private float loseSightTimer;

    public CombatState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        // Debug.Log("<color=red>IA: Entrou no estado de COMBATE!</color>");
        controller.agent.isStopped = false;
        controller.agent.speed = controller.chaseSpeed;
        
        // Zera o timer para permitir ataque imediato se estiver perto
        attackTimer = controller.attackCooldown; 
        loseSightTimer = 0f;
    }

    public void UpdateState()
    {
        if (controller.playerTarget == null)
        {
            controller.ChangeState(EnemyStateID.Patrol);
            return;
        }

        attackTimer += Time.deltaTime;

        // --- Lógica de Perder de Vista ---
        if (!controller.CanSeeTarget())
        {
            loseSightTimer += Time.deltaTime;
            controller.agent.isStopped = false;
            controller.agent.SetDestination(controller.lastSeenLocation);

            if (loseSightTimer >= controller.combatMemoryDuration)
            {
                controller.ChangeState(EnemyStateID.Alert);
            }
            return; 
        }

        loseSightTimer = 0f; 

        // --- Lógica de Combate ---
        float distanceToPlayer = Vector3.Distance(controller.transform.position, controller.playerTarget.position);

        // Verifica se está no range de ataque MELEE
        if (distanceToPlayer <= controller.attackRange)
        {
            StopAndLookAtPlayer();

            if (attackTimer >= controller.attackCooldown)
            {
                TryMeleeAttack(); // <--- MUDANÇA AQUI
                attackTimer = 0f;
            }
        }
        else
        {
            // Persegue
            controller.agent.isStopped = false;
            controller.agent.SetDestination(controller.playerTarget.position);
        }
    }

    private void StopAndLookAtPlayer()
    {
        controller.agent.isStopped = true;
        Vector3 lookDirection = (controller.playerTarget.position - controller.transform.position).normalized;
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }

    private void TryMeleeAttack()
    {
        // Chama a função no Controller que dispara a animação "Punch"
        controller.PerformMeleeAttack();
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        controller.agent.isStopped = true;
    }
}