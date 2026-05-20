using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAnimationEventRelay : MonoBehaviour
{
    #region Runtime

    [SerializeField, HideInInspector] private EnemyAIController owner;

    #endregion

    #region Setup

    // Liga os eventos da animação ao EnemyAIController na raiz do inimigo.
    public void Initialize(EnemyAIController newOwner)
    {
        owner = newOwner;
    }

    #endregion

    #region Animation Events

    // Animation Event: chama no frame de impacto da animação.
    public void Punch()
    {
        owner?.EnableMeleeHitboxFromAnimation();
    }

    // Animation Event: alternativa com nome mais descritivo.
    public void EnableMeleeHitboxFromAnimation()
    {
        owner?.EnableMeleeHitboxFromAnimation();
    }

    // Animation Event: chama no final da janela de dano.
    public void EndPunch()
    {
        owner?.DisableMeleeHitboxFromAnimation();
    }

    // Animation Event: alternativa com nome mais descritivo.
    public void DisableMeleeHitboxFromAnimation()
    {
        owner?.DisableMeleeHitboxFromAnimation();
    }

    #endregion
}
