using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    #region Singleton

    public static QuestManager Instance { get; private set; }

    #endregion

    #region Inspector

    [Header("Database")]
    [Tooltip("Arraste todas as quests criadas no Inspector para o manager conhecê-las.")]
    public List<QuestDefinition> allQuestsDatabase = new List<QuestDefinition>();

    #endregion

    #region Runtime State

    private List<QuestSaveData> activeQuests = new List<QuestSaveData>();
    private List<string> completedQuestIDs = new List<string>();
    private bool eventsSubscribed;

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
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Events

    // Evita inscrição duplicada em eventos globais.
    private void SubscribeEvents()
    {
        if (eventsSubscribed)
            return;

        GameEvents.OnEnemyDeath += HandleEnemyDeath;
        GameEvents.OnItemObtained += HandleItemCollected;
        eventsSubscribed = true;
    }

    // Remove inscrições para evitar progresso duplicado de quest.
    private void UnsubscribeEvents()
    {
        if (!eventsSubscribed)
            return;

        GameEvents.OnEnemyDeath -= HandleEnemyDeath;
        GameEvents.OnItemObtained -= HandleItemCollected;
        eventsSubscribed = false;
    }

    #endregion

    #region Quest Progress

    private void HandleEnemyDeath(string enemyID)
    {
        UpdateQuestProgress(ObjectiveType.Kill, enemyID, 1);
    }

    private void HandleItemCollected(string itemID, int quantity)
    {
        UpdateQuestProgress(ObjectiveType.Collect, itemID, quantity);
    }

    // Atualiza o objetivo atual das quests ativas.
    private void UpdateQuestProgress(ObjectiveType type, string targetID, int amount)
    {
        bool progressMade = false;

        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            QuestSaveData questData = activeQuests[i];

            if (questData == null || questData.isCompleted)
                continue;

            QuestDefinition definition = GetQuestDefinition(questData.questID);

            if (definition == null || definition.steps == null)
                continue;

            if (questData.currentStepIndex < 0 || questData.currentStepIndex >= definition.steps.Count)
                continue;

            QuestStep currentStep = definition.steps[questData.currentStepIndex];

            if (currentStep.type != type || currentStep.targetID != targetID)
                continue;

            questData.currentAmount += amount;
            progressMade = true;

            if (questData.currentAmount < currentStep.amountRequired)
                continue;

            questData.currentStepIndex++;
            questData.currentAmount = 0;

            if (questData.currentStepIndex >= definition.steps.Count)
                CompleteQuest(questData);
        }

        if (progressMade)
            NotifyQuestProgressChanged("Quest Atualizada!", 2f);
    }

    #endregion

    #region Public Quest API

    // Aceita uma quest caso ela ainda não esteja ativa ou completa.
    public void AcceptQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return;

        if (activeQuests.Any(q => q.questID == questID) || completedQuestIDs.Contains(questID))
            return;

        QuestDefinition questDef = GetQuestDefinition(questID);

        if (questDef == null)
        {
            Debug.LogError($"QuestManager: Quest ID {questID} não encontrada no Database.");
            return;
        }

        activeQuests.Add(new QuestSaveData(questID));
        NotifyQuestProgressChanged($"Quest Iniciada: {questDef.title}", 3f);
    }

    // Retorna a definição da quest pelo ID.
    public QuestDefinition GetQuestDefinition(string id)
    {
        if (string.IsNullOrEmpty(id) || allQuestsDatabase == null)
            return null;

        return allQuestsDatabase.Find(q => q != null && q.questID == id);
    }

    #endregion

    #region Completion

    // Finaliza uma quest e aplica recompensas.
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

            NotifyQuestProgressChanged($"Quest Completada: {def.title}!", 4f);
        }
        else
        {
            GameEvents.TriggerQuestProgressChanged();
        }
    }

    #endregion

    #region Save & Load

    public List<QuestSaveData> GetActiveQuestsSaveData()
    {
        return activeQuests ?? new List<QuestSaveData>();
    }

    public List<string> GetCompletedQuestsSaveData()
    {
        return completedQuestIDs ?? new List<string>();
    }

    // Carrega o estado salvo das quests.
    public void LoadQuestData(List<QuestSaveData> active, List<string> completed)
    {
        activeQuests = active ?? new List<QuestSaveData>();
        completedQuestIDs = completed ?? new List<string>();
        GameEvents.TriggerQuestProgressChanged();
    }

    // Limpa o estado runtime das quests.
    public void ResetState()
    {
        activeQuests.Clear();
        completedQuestIDs.Clear();
        GameEvents.TriggerQuestProgressChanged();
    }

    #endregion

    #region Feedback

    // Atualiza UI e feedback de quest.
    private void NotifyQuestProgressChanged(string message, float duration)
    {
        GameEvents.TriggerQuestProgressChanged();

        if (UIFeedbackManager.Instance != null && !string.IsNullOrEmpty(message))
            UIFeedbackManager.Instance.ShowNotification(message, duration);
    }

    #endregion
}
