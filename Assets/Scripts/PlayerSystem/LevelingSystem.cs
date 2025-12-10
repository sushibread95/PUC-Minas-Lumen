using UnityEngine;

public class LevelingSystem : MonoBehaviour
{
    public static LevelingSystem Instance { get; private set; }

    [Header("N�vel Principal")]
    public int currentLevel = 1;
    public float currentBaseXP = 0f;
    public float xpToNextLevel = 100f;

    [Header("XPs Espec�ficos (Acumulativos)")]
    public float combatXPTotal = 0f;      // XP Vermelho (Luta)
    public float purificationXPTotal = 0f; // XP Verde (Stealth/Purificar)

    [Header("Configura��o")]
    public float levelMultiplier = 1.2f; // Dificuldade do pr�ximo n�vel

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AddQuestXP(float amount)
    {
        AddBaseXP(amount);
        Debug.Log($"Ganhou {amount} de XP de Missão (Azul/Neutro).");
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

    // L�gica interna que sobe o n�vel principal
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

        // Aumenta a dificuldade do pr�ximo n�vel
        xpToNextLevel *= levelMultiplier;

        Debug.LogWarning($"LEVEL UP! N�vel atual: {currentLevel}");

        // Atualiza os atributos do Player
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.level = currentLevel;
            PlayerStats.Instance.IncreaseStatsOnLevelUp();
        }

        // Aqui voc� pode tocar som de level up, criar part�culas, etc.
    }
}