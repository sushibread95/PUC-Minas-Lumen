using UnityEngine;

public class AlertState : IEnemyState
{
    private readonly EnemyAIController controller;
    private Vector3 investigationPoint;
    private float alertTimer;
    private const float MaxAlertDuration = 6f;

    public AlertState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        alertTimer = 0f;
        investigationPoint = controller.lastSeenLocation;
    }

    // Investiga o último ponto visto/ouvido e volta à patrulha se não encontrar o player.
    public void UpdateState()
    {
        controller.UpdateAlert(ref alertTimer, ref investigationPoint, MaxAlertDuration);
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        alertTimer = 0f;
    }
}
