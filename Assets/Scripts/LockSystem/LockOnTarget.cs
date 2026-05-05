using UnityEngine;

public class LockOnTarget : MonoBehaviour
{
    #region Inspector - Target

    [Header("ALVO DO LOCK-ON")]
    [Tooltip("Ponto exato que a câmera e a retícula devem seguir. Crie um objeto vazio no peito/cabeça do inimigo e arraste aqui.")]
    [SerializeField] private Transform aimPoint;

    [Tooltip("Prioridade usada na escolha do alvo. Maior prioridade = mais chance de ser escolhido.")]
    public int priority = 0;

    [Tooltip("Se desligado, este inimigo deixa de ser válido para lock-on sem precisar remover o componente.")]
    [SerializeField] private bool isTargetable = true;

    #endregion

    #region Public API

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    // Mantido para compatibilidade com os scripts antigos do ProjetoLumen.
    public Transform Pivot => AimPoint;

    public bool IsTargetable => isTargetable && isActiveAndEnabled && AimPoint != null;

    #endregion

    #region Public Methods

    // Permite que outros scripts desliguem o lock-on deste alvo, por exemplo ao morrer.
    public void SetTargetable(bool value)
    {
        isTargetable = value;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Transform point = AimPoint;
        if (point == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, 0.18f);
        Gizmos.DrawLine(transform.position, point.position);
    }

    #endregion
}
