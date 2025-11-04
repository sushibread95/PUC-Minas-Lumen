using System.Collections.Generic;

[System.Serializable]
public class NPCStateSaveData
{
    public string npcID;
    public NPCState state; 
}

[System.Serializable]
public class InventoryItemSaveData
{
    public string itemID;
    public int quantity;
}

[System.Serializable]
public class GameData
{
    public List<NPCStateSaveData> npcStates;
    public List<InventoryItemSaveData> inventoryItems;

    public GameData()
    {
        npcStates = new List<NPCStateSaveData>();
        inventoryItems = new List<InventoryItemSaveData>();
    }
}