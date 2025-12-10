using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Database")]
    // Arraste todas as suas Quests criadas no Inspector para cá para o Manager conhecê-las
    public List<QuestDefinition> allQuestsDatabase; 

    // Estado Runtime
    private List<QuestSaveData> activeQuests = new List<QuestSaveData>();
    private List<string> completedQuestIDs = new List<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    void OnEnable()
    {
        // Se inscreve nos eventos globais
        GameEvents.OnEnemyDeath += HandleEnemyDeath;
        GameEvents.OnItemObtained += HandleItemCollected;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyDeath -= HandleEnemyDeath;
        GameEvents.OnItemObtained -= HandleItemCollected;
    }

    // --- LÓGICA DE PROGRESSO ---

    private void HandleEnemyDeath(string enemyID)
    {
        UpdateQuestProgress(ObjectiveType.Kill, enemyID, 1);
    }

    private void HandleItemCollected(string itemID, int quantity)
    {
        UpdateQuestProgress(ObjectiveType.Collect, itemID, quantity);
    }

    private void UpdateQuestProgress(ObjectiveType type, string targetID, int amount)
    {
        bool progressMade = false;

        foreach (var questData in activeQuests)
        {
            if (questData.isCompleted) continue;

            QuestDefinition definition = GetQuestDefinition(questData.questID);
            if (definition == null) continue;

            // Pega o passo atual
            if (questData.currentStepIndex >= definition.steps.Count) continue;
            QuestStep currentStep = definition.steps[questData.currentStepIndex];

            // Verifica se o evento corresponde ao objetivo atual
            if (currentStep.type == type && currentStep.targetID == targetID)
            {
                questData.currentAmount += amount;
                
                // Checa se completou o passo
                if (questData.currentAmount >= currentStep.amountRequired)
                {
                    questData.currentStepIndex++;
                    questData.currentAmount = 0; // Reseta contador para o próximo passo
                    
                    // Checa se a quest acabou
                    if (questData.currentStepIndex >= definition.steps.Count)
                    {
                        CompleteQuest(questData);
                    }
                }
                progressMade = true;
            }
        }

        if (progressMade)
        {
            GameEvents.TriggerQuestProgressChanged();
            // Opcional: Tocar SFX ou mostrar Toast
            if (UIFeedbackManager.Instance != null) 
                UIFeedbackManager.Instance.ShowNotification("Quest Atualizada!", 2f);
        }
    }

    public void AcceptQuest(string questID)
    {
        // Evita duplicatas
        if (activeQuests.Any(q => q.questID == questID) || completedQuestIDs.Contains(questID))
        {
            return; 
        }

        QuestDefinition questDef = GetQuestDefinition(questID);
        if (questDef != null)
        {
            activeQuests.Add(new QuestSaveData(questID));
            GameEvents.TriggerQuestProgressChanged();
            
            if (UIFeedbackManager.Instance != null) 
                UIFeedbackManager.Instance.ShowNotification($"Quest Iniciada: {questDef.title}", 3f);
        }
        else
        {
            Debug.LogError($"QuestManager: Quest ID {questID} não encontrada no Database.");
        }
    }

    private void CompleteQuest(QuestSaveData questData)
    {
        questData.isCompleted = true;
        completedQuestIDs.Add(questData.questID);
        activeQuests.Remove(questData); // Move para lista de completas ou mantém com flag? 
        // Nota: Geralmente mantemos em uma lista separada "completed" para limpar o save.

QuestDefinition def = GetQuestDefinition(questData.questID);
        
        // Dar Recompensas
        if (def != null)
        {
            // 1. Dar XP (Agora funciona!)
            if (LevelingSystem.Instance != null && def.xpReward > 0)
            {
                LevelingSystem.Instance.AddQuestXP(def.xpReward);
            }

            // 2. Dar Ouro (Agora funciona!)
            if (PlayerStats.Instance != null && def.goldReward > 0)
            {
                PlayerStats.Instance.AddGold(def.goldReward);
            }

            // 3. Dar Itens (Se tiver InventoryManager)
            // foreach(var itemID in def.itemRewardIDs) InventoryManager.Instance.AddItem(itemID);
            
            if (UIFeedbackManager.Instance != null) 
                UIFeedbackManager.Instance.ShowNotification($"Quest Completada: {def.title}!", 4f);
        }

        GameEvents.TriggerQuestProgressChanged();
    }

    // --- HELPERS ---

    public QuestDefinition GetQuestDefinition(string id)
    {
        return allQuestsDatabase.Find(q => q.questID == id);
    }

    // --- SAVE & LOAD (INTEGRAÇÃO) ---
    
    public List<QuestSaveData> GetActiveQuestsSaveData() => activeQuests;
    public List<string> GetCompletedQuestsSaveData() => completedQuestIDs;

    public void LoadQuestData(List<QuestSaveData> active, List<string> completed)
    {
        activeQuests = active ?? new List<QuestSaveData>();
        completedQuestIDs = completed ?? new List<string>();
        GameEvents.TriggerQuestProgressChanged();
    }

    public void ResetState()
    {
        activeQuests.Clear();
        completedQuestIDs.Clear();
        GameEvents.TriggerQuestProgressChanged();
    }

}