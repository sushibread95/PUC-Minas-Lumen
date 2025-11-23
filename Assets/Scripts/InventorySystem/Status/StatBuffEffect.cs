using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Buff", menuName = "Inventory/Item Effects/Stat Buff")]
public class StatBuffEffect : ItemEffect
{
    // Enum para você escolher qual status melhorar direto no Inspector
    public enum StatType { PhysicalAttack, MagicAttack, Defense, MaxHealth, MaxMana }

    public StatType statToBuff;
    public float amount;

    public override void Apply(GameObject user)
    {
        // Busca o PlayerStats (Onde guardamos os dados de RPG)
        // Nota: Como PlayerStats é Singleton, podemos usar Instance direto, 
        // mas pegar pelo componente é mais seguro se tivermos múltiplos chars no futuro.
        PlayerStats stats = user.GetComponent<PlayerStats>();
        HealthSystem healthSys = user.GetComponent<HealthSystem>();

        if (stats != null)
        {
            switch (statToBuff)
            {
                case StatType.PhysicalAttack:
                    stats.physicalAttack += amount;
                    Debug.Log($"Ataque Físico aumentado em {amount}!");
                    break;
                case StatType.MagicAttack:
                    stats.magicAttack += amount;
                    Debug.Log($"Ataque Mágico aumentado em {amount}!");
                    break;
                case StatType.Defense:
                    stats.defense += amount;
                    Debug.Log($"Defesa aumentada em {amount}!");
                    break;
                case StatType.MaxHealth:
                    stats.maxHealth += amount;
                    // Atualiza a barra visual imediatamente
                    if (healthSys) healthSys.UpdateMaxStats(stats.maxHealth, stats.maxMana);
                    break;
                case StatType.MaxMana:
                    stats.maxMana += amount;
                    // Atualiza a barra visual imediatamente
                    if (healthSys) healthSys.UpdateMaxStats(stats.maxHealth, stats.maxMana);
                    break;
            }
        }
        else
        {
            Debug.LogWarning("StatBuffEffect: PlayerStats não encontrado!");
        }
    }
}