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

    [Header("Atributos de Sobreviv�ncia (Base M�xima)")]
    public float maxHealth = 100f;
    public float maxMana = 50f;

    [Header("Utilit�rios")]
    [Tooltip("Multiplicador de velocidade (1 = normal, 0.5 = 2x mais r�pido)")]
    public float spellCastSpeedMod = 1f;

    [Header("Economia")] // --- NOVO ---
    public int currentGold = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void IncreaseStatsOnLevelUp()
    {
        maxHealth += 10f;
        maxMana += 5f;
        physicalAttack += 2f;
        magicAttack += 2f;
        defense += 1f;

        // Avisa o HealthSystem para curar/atualizar as barras com os novos maximos
        if (HealthSystem.Instance != null)
        {
            HealthSystem.Instance.UpdateMaxStats(maxHealth, maxMana);
        }

        Debug.Log("STATS ATUALIZADOS! Novo MaxHP: " + maxHealth);
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log($"Recebeu {amount} de Ouro! Total: {currentGold}");
        
    }

}