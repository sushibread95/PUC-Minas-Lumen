using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAttackHitbox : MonoBehaviour
{
    #region Inspector

    [Header("Referência")]
    [SerializeField, HideInInspector] private EnemyAIController owner;

    [Header("Detecção")]
    [SerializeField] private bool processTriggerStay = true;

    #endregion

    #region Setup

    // Liga esta hitbox ao EnemyAIController responsável pelo ataque.
    public void Initialize(EnemyAIController newOwner)
    {
        owner = newOwner;
    }

    #endregion

    #region Trigger Events

    private void OnTriggerEnter(Collider other)
    {
        owner?.ProcessMeleeAttackHit(other, this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!processTriggerStay)
            return;

        owner?.ProcessMeleeAttackHit(other, this);
    }

    #endregion
}
