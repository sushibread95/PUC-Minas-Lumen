using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    #region Singleton

    public static QuestManager Instance { get; private set; }

    #endregion

    #region Inspector

    [Header("QUESTS - DATABASE")]
    [Tooltip("Arraste aqui todas as QuestDefinition que o manager deve reconhecer.")]
    [SerializeField] private List<QuestDefinition> allQuestsDatabase = new List<QuestDefinition>();

    #endregion

    #region Runtime State

    private readonly List<QuestSaveData> activeQuests = new List<QuestSaveData>();
    private readonly List<string> completedQuestIDs = new List<string>();
    private bool eventsRegistered;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnregisterEvents();
    }

    #endregion

    #region Event Registration

    // Inscreve o QuestManager nos eventos globais sem duplicar listeners.
    private void RegisterEvents()
    {
        if (eventsRegistered)
            return;

        GameEvents.OnEnemyDeath += HandleEnemyDeath;
        GameEvents.OnItemObtained += HandleItemCollected;
        eventsRegistered = true;
    }

    // Remove inscrições para evitar progresso duplicado após troca de cena/reload.
    private void UnregisterEvents()
    {
        if (!eventsRegistered)
            return;

        GameEvents.OnEnemyDeath -= HandleEnemyDeath;
        GameEvents.OnItemObtained -= HandleItemCollected;
        eventsRegistered = false;
    }

    #endregion

    #region Quest Progress Events

    // Atualiza objetivos de matar inimigos.
    private void HandleEnemyDeath(string enemyID)
    {
        UpdateQuestProgress(ObjectiveType.Kill, enemyID, 1);
    }

    // Atualiza objetivos de coleta de itens.
    private void HandleItemCollected(string itemID, int quantity)
    {
        UpdateQuestProgress(ObjectiveType.Collect, itemID, quantity);
    }

    #endregion

    #region Quest Progress Logic

    // Avança o objetivo atual das quests ativas quando um evento compatível acontece.
    private void UpdateQuestProgress(ObjectiveType type, string targetID, int amount)
    {
        if (string.IsNullOrWhiteSpace(targetID) || amount <= 0)
            return;

        bool progressMade = false;

        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            QuestSaveData questData = activeQuests[i];

            if (questData == null || questData.isCompleted)
                continue;

            QuestDefinition definition = GetQuestDefinition(questData.questID);
            if (definition == null || definition.steps == null || definition.steps.Count == 0)
                continue;

            if (questData.currentStepIndex < 0 || questData.currentStepIndex >= definition.steps.Count)
                continue;

            QuestStep currentStep = definition.steps[questData.currentStepIndex];

            if (currentStep.type != type || currentStep.targetID != targetID)
                continue;

            questData.currentAmount += amount;

            if (questData.currentAmount >= currentStep.amountRequired)
            {
                questData.currentStepIndex++;
                questData.currentAmount = 0;

                if (questData.currentStepIndex >= definition.steps.Count)
                    CompleteQuest(questData);
            }

            progressMade = true;
        }

        if (!progressMade)
            return;

        GameEvents.TriggerQuestProgressChanged();

        if (UIFeedbackManager.Instance != null)
            UIFeedbackManager.Instance.ShowNotification("Quest Atualizada!", 2f);
    }

    // Aceita uma quest se ela existir e ainda não estiver ativa/concluída.
    public void AcceptQuest(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID))
            return;

        if (activeQuests.Any(q => q != null && q.questID == questID) || completedQuestIDs.Contains(questID))
            return;

        QuestDefinition questDef = GetQuestDefinition(questID);
        if (questDef == null)
        {
            Debug.LogError($"QuestManager: Quest ID {questID} não encontrada no Database.");
            return;
        }

        activeQuests.Add(new QuestSaveData(questID));
        GameEvents.TriggerQuestProgressChanged();

        if (UIFeedbackManager.Instance != null)
            UIFeedbackManager.Instance.ShowNotification($"Quest Iniciada: {questDef.title}", 3f);
    }

    // Finaliza a quest, aplica recompensas e atualiza a UI.
    private void CompleteQuest(QuestSaveData questData)
    {
        if (questData == null)
            return;

        questData.isCompleted = true;

        if (!completedQuestIDs.Contains(questData.questID))
            completedQuestIDs.Add(questData.questID);

        activeQuests.Remove(questData);

        QuestDefinition def = GetQuestDefinition(questData.questID);
        if (def != null)
        {
            if (LevelingSystem.Instance != null && def.xpReward > 0)
                LevelingSystem.Instance.AddQuestXP(def.xpReward);

            if (PlayerStats.Instance != null && def.goldReward > 0)
                PlayerStats.Instance.AddGold(def.goldReward);

            if (UIFeedbackManager.Instance != null)
                UIFeedbackManager.Instance.ShowNotification($"Quest Completada: {def.title}!", 4f);
        }

        GameEvents.TriggerQuestProgressChanged();
    }

    #endregion

    #region Lookup

    // Busca uma definição de quest pelo ID.
    public QuestDefinition GetQuestDefinition(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || allQuestsDatabase == null)
            return null;

        return allQuestsDatabase.Find(q => q != null && q.questID == id);
    }

    #endregion

    #region Save And Load

    // Retorna cópia dos dados ativos para o save não modificar a lista interna por referência.
    public List<QuestSaveData> GetActiveQuestsSaveData()
    {
        return new List<QuestSaveData>(activeQuests);
    }

    // Retorna cópia dos IDs concluídos para o save.
    public List<string> GetCompletedQuestsSaveData()
    {
        return new List<string>(completedQuestIDs);
    }

    // Carrega dados de quest salvos e atualiza a interface.
    public void LoadQuestData(List<QuestSaveData> active, List<string> completed)
    {
        activeQuests.Clear();
        completedQuestIDs.Clear();

        if (active != null)
            activeQuests.AddRange(active.Where(q => q != null));

        if (completed != null)
            completedQuestIDs.AddRange(completed.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct());

        GameEvents.TriggerQuestProgressChanged();
    }

    // Limpa progresso de quests para novo jogo/debug.
    public void ResetState()
    {
        activeQuests.Clear();
        completedQuestIDs.Clear();
        GameEvents.TriggerQuestProgressChanged();
    }

    #endregion
}
