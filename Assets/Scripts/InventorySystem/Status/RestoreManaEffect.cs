using UnityEngine;

[CreateAssetMenu(fileName = "New Mana Effect", menuName = "Inventory/Item Effects/Restore Mana")]
public class RestoreManaEffect : ItemEffect
{
    public float manaAmount;

    public override void Apply(GameObject user)
    {
        // Busca o HealthSystem no usuário
        HealthSystem health = user.GetComponent<HealthSystem>();

        if (health != null)
        {
            // Chama a função RestoreMana que criamos no passo anterior
            health.RestoreMana(manaAmount);
            Debug.Log($"<color=blue>Mana recuperada: {manaAmount}</color>");
        }
        else
        {
            Debug.LogWarning("RestoreManaEffect: HealthSystem não encontrado!");
        }
    }
}