// Nome do arquivo: WorldStateManager.cs
// CÓDIGO COMPLETO (COM AS NOVAS FUNÇÕES DE PORTA)

using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    public Dictionary<string, NPCState> npcWorldStates = new Dictionary<string, NPCState>();
    public HashSet<string> collectedItemIDs = new HashSet<string>();
    
    // --- VARIÁVEL ADICIONADA (PARA CORRIGIR O ERRO CS1061) ---
    public HashSet<string> unlockedDoorIDs = new HashSet<string>();
    // --- FIM DA ADIÇÃO ---

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
        
        // --- LINHA ADICIONADA ---
        unlockedDoorIDs.Clear(); // Reseta as portas também
        // --- FIM DA ADIÇÃO ---
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetState();
        }

        // --- MODIFICAÇÃO: Garante que as Quests também sejam resetadas ---
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetState();
        }
        // -----------------------------------------------------------------

        Debug.Log("WorldStateManager RESETADO para Novo Jogo.");
    }
    
    // --- Funções de NPC (você já tinha) ---
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
    
    // --- Funções de Item (você já tinha) ---
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

    // --- FUNÇÕES ADICIONADAS (PARA CORRIGIR O ERRO CS1061) ---

    // 1. Chamada pela InteractableDoor para salvar a porta
    public void RegisterUnlockedDoor(string doorID)
    {
        if (!unlockedDoorIDs.Contains(doorID))
            unlockedDoorIDs.Add(doorID);
    }

    // 2. Chamada pela InteractableDoor para checar o estado no Start()
    public bool IsDoorUnlocked(string doorID)
    {
        return unlockedDoorIDs.Contains(doorID);
    }
    
    // 3. O SaveManager precisa salvar e carregar esta lista
    public List<string> GetDoorSaveData()
    {
        return new List<string>(unlockedDoorIDs); 
    }
    
    public void LoadDoorSaveData(List<string> data)
    {
        if (data != null) unlockedDoorIDs = new HashSet<string>(data); 
        else unlockedDoorIDs = new HashSet<string>();
        Debug.Log("WorldStateManager carregou " + unlockedDoorIDs.Count + " portas destrancadas.");
    }
    // --- FIM DAS FUNÇÕES ADICIONADAS ---
}