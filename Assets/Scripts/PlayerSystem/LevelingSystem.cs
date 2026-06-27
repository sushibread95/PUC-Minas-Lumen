using UnityEngine;
using System;

// Tipo da fonte de XP, usado para colorir o feedback visual (texto flutuante, barras).
public enum XPType { Combat, Purification, Quest }

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

    // Contribuição de cada caminho para o NÍVEL ATUAL (zera ao subir de nível).
    // Alimenta as duas barras: combate (vermelha) e purificação (verde).
    [HideInInspector] public float combatXPThisLevel = 0f;
    [HideInInspector] public float purificationXPThisLevel = 0f;

    // Eventos para o feedback visual (o XPBarUI assina estes).
    public event Action OnStatsChanged;             // qualquer mudança de XP/nível → atualiza barras/nível
    public event Action<float, XPType> OnXPGained;  // ganho pontual → texto flutuante "+X XP"
    public event Action<int> OnLevelUp;             // subiu de nível → flash

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
        OnXPGained?.Invoke(amount, XPType.Quest);
        Debug.Log($"Ganhou {amount} de XP de Missão (Azul/Neutro).");
    }

    // Chamado quando MATA um inimigo
    public void AddCombatXP(float amount)
    {
        combatXPTotal += amount;
        combatXPThisLevel += amount;
        AddBaseXP(amount);
        OnXPGained?.Invoke(amount, XPType.Combat);
        Debug.Log($"Ganhou {amount} de Combat XP (Vermelho).");
    }

    // Chamado quando PURIFICA um inimigo
    public void AddPurificationXP(float amount)
    {
        purificationXPTotal += amount;
        purificationXPThisLevel += amount;
        AddBaseXP(amount);
        OnXPGained?.Invoke(amount, XPType.Purification);
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

        OnStatsChanged?.Invoke();
    }

    private void LevelUp()
    {
        currentBaseXP -= xpToNextLevel;
        currentLevel++;

        // Aumenta a dificuldade do pr�ximo n�vel
        xpToNextLevel *= levelMultiplier;

        // Zera a contribuição por-nível (reinicia as duas barras no novo nível).
        combatXPThisLevel = 0f;
        purificationXPThisLevel = 0f;

        Debug.LogWarning($"LEVEL UP! N�vel atual: {currentLevel}");

        // Atualiza os atributos do Player
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.level = currentLevel;
            PlayerStats.Instance.IncreaseStatsOnLevelUp();
        }

        OnLevelUp?.Invoke(currentLevel);
    }

    #region Save / Load

    // Estado serializável da progressão de XP/nível (usado pelo SaveManager).
    [System.Serializable]
    public class LevelingSaveData
    {
        public int currentLevel = 1;
        public float currentBaseXP = 0f;
        public float xpToNextLevel = 100f;
        public float combatXPTotal = 0f;
        public float purificationXPTotal = 0f;
        public float combatXPThisLevel = 0f;
        public float purificationXPThisLevel = 0f;
    }

    public LevelingSaveData GetSaveData()
    {
        return new LevelingSaveData
        {
            currentLevel = currentLevel,
            currentBaseXP = currentBaseXP,
            xpToNextLevel = xpToNextLevel,
            combatXPTotal = combatXPTotal,
            purificationXPTotal = purificationXPTotal,
            combatXPThisLevel = combatXPThisLevel,
            purificationXPThisLevel = purificationXPThisLevel
        };
    }

    public void LoadSaveData(LevelingSaveData data)
    {
        if (data == null) return;
        currentLevel = data.currentLevel;
        currentBaseXP = data.currentBaseXP;
        xpToNextLevel = data.xpToNextLevel;
        combatXPTotal = data.combatXPTotal;
        purificationXPTotal = data.purificationXPTotal;
        combatXPThisLevel = data.combatXPThisLevel;
        purificationXPThisLevel = data.purificationXPThisLevel;

        // Atualiza as barras/HUD após carregar.
        OnStatsChanged?.Invoke();
    }

    #endregion
}