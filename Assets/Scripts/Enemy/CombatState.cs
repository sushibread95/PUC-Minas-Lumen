// CombatState.cs
using UnityEngine;
using UnityEngine.AI;

public class CombatState : IEnemyState
{
    private readonly EnemyAIController controller;
    private float attackTimer;

    // --- NOVA LINHA ---
    private float loseSightTimer; // O timer da "Memória de Combate"

    public CombatState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        Debug.Log("<color=red>IA: Entrou no estado de COMBATE!</color>");
        controller.agent.isStopped = false;
        controller.agent.speed = controller.chaseSpeed;
        attackTimer = 0f;
        loseSightTimer = 0f; // Reseta o timer de "perder de vista"
    }

    public void UpdateState()
    {
        if (controller.playerTarget == null)
        {
            controller.ChangeState(EnemyStateID.Patrol);
            return;
        }

        attackTimer += Time.deltaTime;

        // --- INÍCIO DA MODIFICAÇÃO (Memória de Combate) ---
        if (!controller.CanSeeTarget())
        {
            // O Player se escondeu! Inicia o timer.
            loseSightTimer += Time.deltaTime;

            // Continua perseguindo a ÚLTIMA POSIÇÃO VISTA
            controller.agent.isStopped = false;
            controller.agent.SetDestination(controller.lastSeenLocation);

            if (loseSightTimer >= controller.combatMemoryDuration)
            {
                // Tempo esgotado! Desiste e volta a procurar (Alerta)
                controller.ChangeState(EnemyStateID.Alert);
            }
            return; // Pula a lógica de ataque
        }
        // --- FIM DA MODIFICAÇÃO ---

        // Se chegou aqui, o player ESTÁ visível
        loseSightTimer = 0f; // Reseta o timer de "perder de vista"

        // (Resto do código de combate é o mesmo)
        float distanceToPlayer = Vector3.Distance(controller.transform.position, controller.playerTarget.position);

        if (distanceToPlayer <= controller.attackRange)
        {
            StopAndLookAtPlayer();
            if (attackTimer >= controller.attackCooldown)
            {
                TryAttack();
                attackTimer = 0f;
            }
        }
        else
        {
            controller.agent.isStopped = false;
            controller.agent.SetDestination(controller.playerTarget.position);
        }
    }

    private void StopAndLookAtPlayer()
    {
        // ... (código sem alteração) ...
        controller.agent.isStopped = true;
        Vector3 lookDirection = (controller.playerTarget.position - controller.transform.position).normalized;
        lookDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        controller.transform.rotation = Quaternion.Slerp(
            controller.transform.rotation,
            targetRotation,
            Time.deltaTime * 5f
        );
    }

    private void TryAttack()
    {
        // ... (código sem alteração) ...
        if (controller.cannon == null || controller.enemyAttacks == null || controller.enemyAttacks.Length == 0)
        {
            Debug.LogWarning("IA: Tentei atacar, mas não tenho Cannon ou 'Enemy Attacks' configurados.");
            return;
        }
        Spell attackToUse = controller.enemyAttacks[0];
        Debug.Log("IA: Atirando!");
        controller.cannon.Fire(attackToUse);
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        controller.agent.isStopped = true;
    }
}