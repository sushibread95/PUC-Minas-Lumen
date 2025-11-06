using System.Collections.Generic;

// --- ESTRUTURA PARA SALVAR O ESTADO DO NPC ---
[System.Serializable]
public class NPCStateSaveData
{
    public string npcID;
    public NPCState state; // Assume que NPCState é um enum definido em GameEnums.cs
}

// --- ESTRUTURA PARA SALVAR O INVENTÁRIO (NOVA) ---
[System.Serializable]
public class InventoryItemSaveData
{
    public string itemID; // O objectName do ScriptableObject
    public int quantity;
}

// --- O ARQUIVO DE SAVE PRINCIPAL (ATUALIZADO) ---
[System.Serializable]
public class GameData
{
    public List<NPCStateSaveData> npcStates;
    public List<InventoryItemSaveData> inventoryItems; // <-- MODIFICAÇÃO NECESSÁRIA
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public List<string> collectedItemIDs; 

    public GameData()
    {
        npcStates = new List<NPCStateSaveData>();
        inventoryItems = new List<InventoryItemSaveData>(); // <-- MODIFICAÇÃO NECESSÁRIA
        collectedItemIDs = new List<string>();
        playerPosX = 0; 
        playerPosY = 0;
        playerPosZ = 0;
    }
}