using UnityEngine;

public class FallenState : IEnemyState
{
    private readonly EnemyAIController controller;
    private float fallenTimer;

    public FallenState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        fallenTimer = 0f;
        controller.AbortCombat();

        if (controller.lockOnTarget != null)
            controller.lockOnTarget.SetTargetable(false);

        if (controller.enemyHealth != null && controller.enemyHealth.healthBarSlider != null)
            controller.enemyHealth.healthBarSlider.gameObject.SetActive(false);

        if (controller.npcData != null)
            controller.npcData.EntrarEmNocaute();
    }

    // Mantém o inimigo caído até o tempo acabar, então ele se recupera.
    public void UpdateState()
    {
        fallenTimer += Time.deltaTime;

        if (fallenTimer < controller.fallenDuration)
            return;

        controller.enemyHealth.RecoverFromFallen();
        controller.ChangeState(EnemyStateID.Combat);
    }

    public void FixedUpdateState() { }

    public void ExitState()
    {
        if (controller.lockOnTarget != null && controller.enemyHealth != null && !controller.enemyHealth.isDead)
            controller.lockOnTarget.SetTargetable(true);
    }
}
