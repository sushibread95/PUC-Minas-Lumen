using UnityEngine;

[CreateAssetMenu(fileName = "New XP Effect", menuName = "Inventory/Item Effects/Give XP")]
public class ExperienceEffect : ItemEffect
{
    public float xpAmount;

    public override void Apply(GameObject user)
    {

        if (LevelingSystem.Instance != null)
        {
            // Adiciona como XP de Combate (ou crie um método genérico no LevelingSystem se preferir)
            LevelingSystem.Instance.AddCombatXP(xpAmount);
            Debug.Log($"<color=yellow>Ganhou {xpAmount} de XP via Item!</color>");
        }
        else
        {
            Debug.LogWarning("LevelingSystem não encontrado na cena!");
        }
    }
}