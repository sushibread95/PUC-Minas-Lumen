using UnityEngine;
// using static Unity.Cinemachine.Samples.PlatformerCamera2D; // Você não precisa desta linha aqui

[CreateAssetMenu(fileName = "New Heal Effect", menuName = "Inventory/Item Effects/Heal")]
public class HealEffect : ItemEffect
{
    public int healAmount; // Mude para 'float' se quiser cura decimal, ex: 25.5

    public override void Apply(GameObject user)
    {
        // --- INÍCIO DA CORREÇÃO ---
        // 1. Procura o script CORRETO (HealthSystem)
        HealthSystem health = user.GetComponent<HealthSystem>();

        if (health != null)
        {
            // 2. Chama a função CORRETA (que atualiza o slider)
            health.RecoverHealth(healAmount);
            Debug.Log($"Curou {healAmount} de vida do jogador.");
        }
        else
        {
            // Se falhar, é porque o PlayerStats ainda está no caminho
            Debug.LogError($"HealEffect falhou: Não encontrou o script 'HealthSystem' no objeto {user.name}!");
        }
        // --- FIM DA CORREÇÃO ---
    }
}