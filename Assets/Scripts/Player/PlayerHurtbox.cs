using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerHurtbox : MonoBehaviour
{
    [Tooltip("A referência para o sistema de vida principal na raiz do Player.")]
    public HealthSystem mainHealthSystem;

    void Awake()
    {
        // Acha o chefe automaticamente
        if (mainHealthSystem == null)
        {
            mainHealthSystem = GetComponentInParent<HealthSystem>();
        }

        // Garante que é um Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    // A mágica: Quando a espada do inimigo entrar NESTA caixa...
    private void OnTriggerEnter(Collider other)
    {
        if (mainHealthSystem != null)
        {
            // ...nós pegamos a espada (GameObject) e repassamos direto pro sistema complexo do Player!
            mainHealthSystem.HandleHit(other.gameObject);
        }
    }
}