using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // --- CORREÇÃO (Save de Inventário) ---
    // O campo era 'object', que o JsonUtility NÃO serializa — o inventário
    // nunca chegava ao savegame.json. Agora usa o tipo concreto e serializável
    // do InventoryManager.
    public InventoryManager.InventorySaveData inventoryItems;

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

    // --- ADIÇÃO (#3 persistência): progressão do player (XP/nível e atributos/gold) ---
    public LevelingSystem.LevelingSaveData levelingData;
    public PlayerStats.PlayerStatsSaveData playerStats;
    // ---------------------------------------------------------------------------------

    public GameData()
    {
        playerPosX = 0; 
        playerPosY = 0; 
        playerPosZ = 0; 
        
        npcStates = new List<NPCStateSaveData>();
        collectedItemIDs = new List<string>();
        inventoryItems = new InventoryManager.InventorySaveData();

        unlockedDoorIDs = new List<string>();
        
        // --- INICIALIZAÇÃO ---
        triggeredEvents = new List<string>();

        levelingData = new LevelingSystem.LevelingSaveData();
        playerStats = new PlayerStats.PlayerStatsSaveData();
    }
}

[System.Serializable]
public class NPCStateSaveData
{
    public string npcID;
    public NPCState state;
}