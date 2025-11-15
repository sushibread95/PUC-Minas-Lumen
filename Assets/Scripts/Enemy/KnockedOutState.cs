using UnityEngine;

public class KnockedOutState : IEnemyState
{
    private readonly EnemyAIController controller;

    public KnockedOutState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.agent.isStopped = true;
        if (controller.lockOnTarget != null)
        {
            controller.lockOnTarget.enabled = false;
        }
    }

    public void UpdateState() { }
    public void FixedUpdateState() { }
    public void ExitState() { }
}