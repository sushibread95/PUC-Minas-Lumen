using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // --- AVISO DE MODIFICAÇÃO ---
    // A linha 'public List<InventoryItemSaveData> inventoryItems;' foi
    // substituída por 'public object inventoryItems;' para ser
    // compatível com o NOVO InventoryManager.cs.
    public object inventoryItems;

    public List<NPCStateSaveData> npcStates;
    public List<string> collectedItemIDs;

    public List<QuestSaveData> activeQuests = new List<QuestSaveData>();
    
    public List<string> completedQuestIDs = new List<string>();
    
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    
    public List<string> unlockedDoorIDs;

    // --- ADIÇÃO: Lista para salvar diálogos/eventos já vistos ---
    public List<string> triggeredEvents = new List<string>();
    // -----------------------------------------------------------

    public GameData()
    {
        playerPosX = 0; 
        playerPosY = 0; 
        playerPosZ = 0; 
        
        npcStates = new List<NPCStateSaveData>();
        collectedItemIDs = new List<string>();
        inventoryItems = null;

        unlockedDoorIDs = new List<string>();
        
        // --- INICIALIZAÇÃO ---
        triggeredEvents = new List<string>();
    }
}

[System.Serializable]
public class NPCStateSaveData
{
    public string npcID;
    public NPCState state;
}