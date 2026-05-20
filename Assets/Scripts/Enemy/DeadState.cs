public class DeadState : IEnemyState
{
    private readonly EnemyAIController controller;

    public DeadState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.AbortCombat();
        if (controller.lockOnTarget != null)
            controller.lockOnTarget.SetTargetable(false);
        controller.enabled = false;
    }

    public void UpdateState() { }
    public void FixedUpdateState() { }
    public void ExitState() { }
}
