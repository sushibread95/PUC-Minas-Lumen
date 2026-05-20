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
        if (controller.agent != null)
        {
            controller.agent.isStopped = false;
            controller.agent.speed = controller.chaseSpeed;
        }

        controller.ResetAttackTimer();
        attackTimer = 0f;
        loseSightTimer = 0f;
    }

    // Executa combate melee, ranged ou grabber conforme o tipo definido no EnemyAIController.
    public void UpdateState()
    {
        controller.UpdateCombat(ref attackTimer, ref loseSightTimer);
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        if (controller.agent != null && controller.agent.enabled && controller.agent.isOnNavMesh)
            controller.agent.isStopped = true;
    }
}
