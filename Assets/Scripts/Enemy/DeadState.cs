using UnityEngine;

public class DeadState : IEnemyState
{
    private readonly EnemyAIController controller;

    public DeadState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.agent.isStopped = true;
        controller.enabled = false;
        if (controller.lockOnTarget != null)
        {
            controller.lockOnTarget.enabled = false;
        }
    }

    public void UpdateState() { }
    public void FixedUpdateState() { }
    public void ExitState() { }
}