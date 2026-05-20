public class PatrolState : IEnemyState
{
    private readonly EnemyAIController controller;

    public PatrolState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        if (controller.agent == null)
            return;

        controller.agent.isStopped = false;
        controller.agent.speed = controller.walkSpeed;
    }

    // Delega a patrulha ao controller para manter um único lugar de configuração.
    public void UpdateState()
    {
        controller.UpdatePatrol();
    }

    public void FixedUpdateState() { }

    public void ExitState() { }
}
