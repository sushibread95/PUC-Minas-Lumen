using UnityEngine;

public class UniversalHurtbox : MonoBehaviour
{
    public Team myTeam;
    
    [Header("Detected Components")]
    private HealthSystem playerHealth;   // Usado se for o Player
    private EnemyHealth enemyHealth;     // Usado se for o Inimigo
    private PlayerControllerSystem playerController;
    private EnemyAIController enemyAI;

    void Awake()
    {
        // Tenta pegar os dois. O que for nulo, a gente ignora depois.
        playerHealth = GetComponentInParent<HealthSystem>();
        enemyHealth = GetComponentInParent<EnemyHealth>();
        
        playerController = GetComponentInParent<PlayerControllerSystem>();
        enemyAI = GetComponentInParent<EnemyAIController>();
    }

    public void TakeHit(DamagePacket packet)
    {
        // 1. Regra de Ouro: Fogo Amigo
        if (packet.team == myTeam) return;

        // --- LÓGICA PARA PLAYER ---
        if (playerHealth != null)
        {
            if (playerHealth.isDead) return;
            
            // Checa esquiva no cérebro de movimento
            if (playerController != null && playerController.IsInvulnerable()) 
            {
                Debug.Log("🛡️ Player desviou do golpe!");
                return;
            }

            playerHealth.ApplyEffect(packet.effects);
        }

        // --- LÓGICA PARA INIMIGO ---
        if (enemyHealth != null)
        {
            if (enemyHealth.isDead || enemyHealth.isFallen) return;

            // Se a IA diz que já foi purificado, ignora dano
            if (enemyAI != null && enemyAI.isPurified) return;

            enemyHealth.ApplyEffect(packet.effects);
        }
        
        Debug.Log($"<color=orange>{transform.root.name}</color> atingido por <color=red>{packet.attacker.name}</color>");
    }
}