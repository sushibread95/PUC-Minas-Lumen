using UnityEngine;

public class LevelingSystem : MonoBehaviour
{
    public static LevelingSystem Instance { get; private set; }

    [Header("Nível Principal")]
    public int currentLevel = 1;
    public float currentBaseXP = 0f;
    public float xpToNextLevel = 100f;

    [Header("XPs Específicos (Acumulativos)")]
    public float combatXPTotal = 0f;      // XP Vermelho (Luta)
    public float purificationXPTotal = 0f; // XP Verde (Stealth/Purificar)

    [Header("Configuração")]
    public float levelMultiplier = 1.2f; // Dificuldade do próximo nível

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Chamado quando MATA um inimigo
    public void AddCombatXP(float amount)
    {
        combatXPTotal += amount;
        AddBaseXP(amount);
        Debug.Log($"Ganhou {amount} de Combat XP (Vermelho).");
    }

    // Chamado quando PURIFICA um inimigo
    public void AddPurificationXP(float amount)
    {
        purificationXPTotal += amount;
        AddBaseXP(amount);
        Debug.Log($"Ganhou {amount} de Purification XP (Verde).");
    }

    // Lógica interna que sobe o nível principal
    private void AddBaseXP(float amount)
    {
        currentBaseXP += amount;

        while (currentBaseXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentBaseXP -= xpToNextLevel;
        currentLevel++;

        // Aumenta a dificuldade do próximo nível
        xpToNextLevel *= levelMultiplier;

        Debug.LogWarning($"LEVEL UP! Nível atual: {currentLevel}");

        // Atualiza os atributos do Player
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.level = currentLevel;
            PlayerStats.Instance.IncreaseStatsOnLevelUp();
        }

        // Aqui você pode tocar som de level up, criar partículas, etc.
    }
}