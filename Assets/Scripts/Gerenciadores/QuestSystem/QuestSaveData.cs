using System;

[System.Serializable]
public class QuestSaveData
{
    public string questID;          // Link para o ScriptableObject
    public int currentStepIndex;    // Qual passo o player está?
    public int currentAmount;       // Quanto já progrediu no passo atual?
    public bool isCompleted;        // Já terminou?
    
    public QuestSaveData(string id)
    {
        questID = id;
        currentStepIndex = 0;
        currentAmount = 0;
        isCompleted = false;
    }
}