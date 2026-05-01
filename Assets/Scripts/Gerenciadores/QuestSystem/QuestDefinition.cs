using UnityEngine;
using System.Collections.Generic;

public enum QuestType { Main, Side }
public enum ObjectiveType { Kill, Collect, Talk, Visit }

[System.Serializable]
public class QuestStep
{
    public string description;      // Ex: "Mate 5 Slimes"
    public ObjectiveType type;      // Kill
    public string targetID;         // "Slime_Green" ou "HealthPotion"
    public int amountRequired;      // 5
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quests/Quest Definition")]
public class QuestDefinition : ScriptableObject
{
    [Header("Info")]
    public string questID; // ID ÚNICO (Ex: "quest_intro_01")
    public string title;
    [TextArea] public string description;
    public QuestType questType;

    [Header("Steps")]
    public List<QuestStep> steps;

    [Header("Rewards")]
    public int goldReward;
    public int xpReward;
    public List<string> itemRewardIDs; // IDs para adicionar ao inventário
    
    // Opcional: Próxima quest automática
    public QuestDefinition nextQuest;
}