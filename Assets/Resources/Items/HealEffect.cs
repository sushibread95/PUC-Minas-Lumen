using UnityEngine;
using static Unity.Cinemachine.Samples.PlatformerCamera2D;

[CreateAssetMenu(fileName = "New Heal Effect", menuName = "Inventory/Item Effects/Heal")]
public class HealEffect : ItemEffect
{
    public int healAmount;

    public override void Apply(GameObject user)
    {
        // Você precisará de um script no seu jogador (ex: PlayerHealth)
        // para receber esta cura.
        PlayerStats stats = user.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.Heal(healAmount);
            Debug.Log($"Curou {healAmount} de vida do jogador.");
        }
    }
}