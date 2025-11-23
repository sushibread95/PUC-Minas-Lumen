using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Atributos Principais")]
    public int level = 1;

    [Header("Atributos de Combate")]
    public float physicalAttack = 10f;
    public float magicAttack = 10f;
    public float defense = 5f;

    [Header("Atributos de Sobrevivência (Base Máxima)")]
    public float maxHealth = 100f;
    public float maxMana = 50f;

    [Header("Utilitários")]
    [Tooltip("Multiplicador de velocidade (1 = normal, 0.5 = 2x mais rápido)")]
    public float spellCastSpeedMod = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Função chamada pelo LevelingSystem ao upar
    public void IncreaseStatsOnLevelUp()
    {
        // Exemplo de curva de crescimento simples
        maxHealth += 10f;
        maxMana += 5f;
        physicalAttack += 2f;
        magicAttack += 2f;
        defense += 1f;

        // Avisa o HealthSystem para curar/atualizar as barras com os novos máximos
        if (HealthSystem.Instance != null)
        {
            HealthSystem.Instance.UpdateMaxStats(maxHealth, maxMana);
        }

        Debug.Log("STATS ATUALIZADOS! Novo MaxHP: " + maxHealth);
    }

    // Futuramente aqui teremos métodos como:
    // public void EquipItem(Item item) { ... }
}