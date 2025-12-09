using UnityEngine;
using UnityEngine.AI;

// CÓDIGO ATUALIZADO
public class FallenState : IEnemyState
{
    private readonly EnemyAIController controller;

    // --- INÍCIO DAS MUDANÇAS ---
    private float fallenTimer;
    // --- FIM DAS MUDANÇAS ---

    public FallenState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        // --- INÍCIO DAS MUDANÇAS ---
        fallenTimer = 0f; // Reseta o timer
        // --- FIM DAS MUDANÇAS ---

        controller.agent.isStopped = true;
        controller.agent.velocity = Vector3.zero;

        if (controller.lockOnTarget != null)
        {
            controller.lockOnTarget.enabled = false;
        }

        // Esconde a barra de HP para o player não saber a vida dele
        if (controller.enemyHealth.healthBarSlider != null)
        {
            controller.enemyHealth.healthBarSlider.gameObject.SetActive(false);
        }

        // Avisa o CorruptedNPC para entrar em "Nocaute" (para a UI aparecer)
        if (controller.npcData != null)
        {
            controller.npcData.EntrarEmNocaute();
        }
    }

    // --- INÍCIO DAS MUDANÇAS ---
    public void UpdateState()
    {
        fallenTimer += Time.deltaTime;

        // Tempo esgotado!
        if (fallenTimer >= controller.fallenDuration)
        {
            Debug.Log("Inimigo se levantou!");

            // 1. "Acorda" o script de vida (cura um pouco e permite a morte)
            controller.enemyHealth.RecoverFromFallen();

            // 2. Volta para o estado de Combate
            controller.ChangeState(EnemyStateID.Combat);
        }
    }
    // --- FIM DAS MUDANÇAS ---

    public void FixedUpdateState() { }

    public void ExitState()
    {
        // Garante que o Lock-On seja reativado ao sair do estado
        if (controller.lockOnTarget != null)
        {
            controller.lockOnTarget.enabled = true;
        }

        // Garante que a UI de escolha suma
        // (O NPCInteraction.cs deve cuidar disso, mas é uma segurança)
    }
}