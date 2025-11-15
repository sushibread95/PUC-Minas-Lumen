using UnityEngine;
using UnityEngine.AI;

public class AlertState : IEnemyState
{
    private readonly EnemyAIController controller;
    private Vector3 investigationPoint;
    private float alertTimer = 0f;
    private float maxAlertDuration = 6f;

    public AlertState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        controller.agent.isStopped = false;
        controller.agent.speed = controller.walkSpeed;
        alertTimer = 0f;
        investigationPoint = controller.lastSeenLocation;
        controller.agent.SetDestination(investigationPoint);
    }

    public void UpdateState()
    {
        if (controller.CanSeeTarget())
        {
            controller.ChangeState(EnemyStateID.Combat);
            return;
        }

        if (!controller.agent.pathPending && controller.agent.remainingDistance < 1.0f)
        {
            controller.transform.Rotate(0, 70 * Time.deltaTime, 0);
            alertTimer += Time.deltaTime;
        }

        if (alertTimer >= maxAlertDuration)
        {
            controller.ChangeState(EnemyStateID.Patrol);
        }

        if (controller.CanHearTarget() && controller.lastSeenLocation != investigationPoint)
        {
            investigationPoint = controller.lastSeenLocation;
            controller.agent.SetDestination(investigationPoint);
            alertTimer = 0f;
        }
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        controller.agent.isStopped = true;
        alertTimer = 0f;
    }
}