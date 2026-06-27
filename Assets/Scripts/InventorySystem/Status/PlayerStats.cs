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

    #region Save / Load

    // Estado serializável dos atributos/economia do player (usado pelo SaveManager).
    [System.Serializable]
    public class PlayerStatsSaveData
    {
        public int level = 1;
        public float physicalAttack = 10f;
        public float magicAttack = 10f;
        public float defense = 5f;
        public float maxHealth = 100f;
        public float maxMana = 50f;
        public float spellCastSpeedMod = 1f;
        public int currentGold = 0;
    }

    public PlayerStatsSaveData GetSaveData()
    {
        return new PlayerStatsSaveData
        {
            level = level,
            physicalAttack = physicalAttack,
            magicAttack = magicAttack,
            defense = defense,
            maxHealth = maxHealth,
            maxMana = maxMana,
            spellCastSpeedMod = spellCastSpeedMod,
            currentGold = currentGold
        };
    }

    public void LoadSaveData(PlayerStatsSaveData data)
    {
        if (data == null) return;
        level = data.level;
        physicalAttack = data.physicalAttack;
        magicAttack = data.magicAttack;
        defense = data.defense;
        maxHealth = data.maxHealth;
        maxMana = data.maxMana;
        spellCastSpeedMod = data.spellCastSpeedMod;
        currentGold = data.currentGold;

        // Atualiza HUD e máximos no HealthSystem com os valores carregados.
        if (HealthSystem.Instance != null)
            HealthSystem.Instance.UpdateMaxStats(maxHealth, maxMana);
    }

    #endregion

}