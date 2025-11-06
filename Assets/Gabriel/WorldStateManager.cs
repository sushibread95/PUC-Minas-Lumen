using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    public Dictionary<string, NPCState> npcWorldStates = new Dictionary<string, NPCState>();
    public HashSet<string> collectedItemIDs = new HashSet<string>();

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
        
        // --- MODIFICAÇÃO NECESSÁRIA ---
        // Também reseta o inventário
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ResetState();
        }
        // --- FIM DA MODIFICAÇÃO ---
        
        Debug.Log("WorldStateManager RESETADO para Novo Jogo.");
    }
    
    public void SetNPCState(string npcID, NPCState state)
    {
        if (string.IsNullOrEmpty(npcID)) return;
        npcWorldStates[npcID] = state;
        Debug.Log("Estado do Mundo Salvo: " + npcID + " agora é " + state);
    }
    public void RegisterCollectedItem(string id)
    {
        if (!collectedItemIDs.Contains(id))
            collectedItemIDs.Add(id);
    }
    public bool IsItemCollected(string id)
    {
        return collectedItemIDs.Contains(id);
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
        Debug.Log("WorldStateManager carregou " + npcWorldStates.Count + " estados de NPC.");
    }
    public List<string> GetItemSaveData()
    {
        return new List<string>(collectedItemIDs); 
    }
    public void LoadItemSaveData(List<string> data)
    {
        if (data != null) collectedItemIDs = new HashSet<string>(data); 
        else collectedItemIDs = new HashSet<string>();
        Debug.Log("WorldStateManager carregou " + collectedItemIDs.Count + " itens de cena coletados.");
    }
}