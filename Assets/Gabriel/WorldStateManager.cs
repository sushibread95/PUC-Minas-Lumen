using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    // --- ESTADOS DO MUNDO ---
    public Dictionary<string, NPCState> npcWorldStates = new Dictionary<string, NPCState>();
    public HashSet<string> collectedItemIDs = new HashSet<string>();
    
    // Lista de portas destrancadas (para persistência de chaves)
    public HashSet<string> unlockedDoorIDs = new HashSet<string>();
    
    // Lista de eventos/diálogos já acionados (para não repetirem)
    public HashSet<string> triggeredEventIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ResetState()
    {
        npcWorldStates.Clear();
        collectedItemIDs.Clear(); 
        unlockedDoorIDs.Clear(); 
        triggeredEventIDs.Clear(); // Limpa eventos também
        
        if (InventoryManager.Instance != null) InventoryManager.Instance.ResetState();
        if (QuestManager.Instance != null) QuestManager.Instance.ResetState();

        Debug.Log("WorldStateManager RESETADO para Novo Jogo.");
    }
    
    // --- FUNÇÕES DE NPC ---
    public void SetNPCState(string npcID, NPCState state)
    {
        if (string.IsNullOrEmpty(npcID)) return;
        npcWorldStates[npcID] = state;
    }

    public List<NPCStateSaveData> GetSaveData()
    {
        List<NPCStateSaveData> dataParaSalvar = new List<NPCStateSaveData>();
        foreach (var par in npcWorldStates)
        {
            dataParaSalvar.Add(new NPCStateSaveData { npcID = par.Key, state = par.Value });
        }
        return dataParaSalvar;
    }

    public void LoadSaveData(List<NPCStateSaveData> dadosCarregados)
    {
        npcWorldStates.Clear();
        if (dadosCarregados == null) return;
        foreach (var item in dadosCarregados)
        {
             if (!string.IsNullOrEmpty(item.npcID))
                 npcWorldStates[item.npcID] = item.state;
        }
    }
    
    // --- FUNÇÕES DE ITENS COLETADOS ---
    public void RegisterCollectedItem(string id)
    {
        if (!collectedItemIDs.Contains(id))
            collectedItemIDs.Add(id);
    }

    public bool IsItemCollected(string id)
    {
        return collectedItemIDs.Contains(id);
    }

    public List<string> GetItemSaveData()
    {
        return new List<string>(collectedItemIDs); 
    }

    public void LoadItemSaveData(List<string> data)
    {
        if (data != null) collectedItemIDs = new HashSet<string>(data); 
        else collectedItemIDs = new HashSet<string>();
    }

    // --- FUNÇÕES DE PORTAS (Destrancadas) ---
    public void RegisterUnlockedDoor(string doorID)
    {
        if (!unlockedDoorIDs.Contains(doorID))
            unlockedDoorIDs.Add(doorID);
    }

    public bool IsDoorUnlocked(string doorID)
    {
        return unlockedDoorIDs.Contains(doorID);
    }
    
    public List<string> GetDoorSaveData()
    {
        return new List<string>(unlockedDoorIDs); 
    }
    
    public void LoadDoorSaveData(List<string> data)
    {
        if (data != null) unlockedDoorIDs = new HashSet<string>(data); 
        else unlockedDoorIDs = new HashSet<string>();
    }

    // --- FUNÇÕES DE EVENTOS/DIÁLOGOS (Triggered Events) ---
    public void RegisterEventTriggered(string eventID)
    {
        if (!string.IsNullOrEmpty(eventID) && !triggeredEventIDs.Contains(eventID))
        {
            triggeredEventIDs.Add(eventID);
        }
    }

    public bool HasEventHappened(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return triggeredEventIDs.Contains(eventID);
    }

    public List<string> GetTriggeredEventsSaveData()
    {
        return new List<string>(triggeredEventIDs);
    }

    public void LoadTriggeredEventsSaveData(List<string> data)
    {
        if (data != null) triggeredEventIDs = new HashSet<string>(data);
        else triggeredEventIDs = new HashSet<string>();
    }
}