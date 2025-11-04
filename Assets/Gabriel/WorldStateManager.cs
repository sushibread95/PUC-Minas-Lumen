using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    // O "caderno" mestre de todas as decisões sobre NPCs.
    public Dictionary<string, NPCState> npcWorldStates = new Dictionary<string, NPCState>();

    // (Adicionar outros Dictionaries aqui para outros estados do mundo, se necessário,
    //  ex: Dictionary<string, bool> doorsUnlocked = new Dictionary<string, bool>(); )

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // --- NOVA FUNÇÃO DE RESET ---
    public void ResetState()
    {
        npcWorldStates.Clear(); 
        
        if (InventoryManager.Instance != null)
    {
        InventoryManager.Instance.ResetState();
    }

        Debug.Log("WorldStateManager RESETADO para Novo Jogo.");
    }
    // ----------------------------

    public void SetNPCState(string npcID, NPCState state)
    {
        if (string.IsNullOrEmpty(npcID))
        {
             Debug.LogWarning("Tentativa de salvar estado de NPC com ID vazio!");
             return;
        }

        if (npcWorldStates.ContainsKey(npcID))
        {
            npcWorldStates[npcID] = state; // Atualiza o estado
        }
        else
        {
            npcWorldStates.Add(npcID, state); // Adiciona pela primeira vez
        }
        Debug.Log("Estado do Mundo Salvo: " + npcID + " agora é " + state);
    }

    // (Adicionar funções Set... para outros estados do mundo aqui, se necessário)
    // public void SetDoorState(string doorID, bool unlocked) { ... }


    // --- FUNÇÕES DE SAVE/LOAD ---
    // (Note que você tinha duas versões de GetSaveData/LoadSaveData.
    //  O SaveManager.cs atual usa a versão com List<NPCStateSaveData>.
    //  Removi as versões com Dictionary para evitar confusão.)

    // O SaveManager chama isso para PEGAR os dados
    public List<NPCStateSaveData> GetSaveData()
    {
        List<NPCStateSaveData> dataParaSalvar = new List<NPCStateSaveData>();

        // Converte nosso Dictionary (rápido) para uma Lista (salvável)
        foreach (var par in npcWorldStates)
        {
            dataParaSalvar.Add(new NPCStateSaveData
            {
                npcID = par.Key,
                state = par.Value
            });
        }
        // (Adicionar conversão de outros Dictionaries para Listas aqui no futuro)
        return dataParaSalvar;
    }

    // O SaveManager chama isso para ENTREGAR os dados
    public void LoadSaveData(List<NPCStateSaveData> dadosCarregados)
    {
        // Limpa o Dictionary atual
        npcWorldStates.Clear();
        // (Limpar outros Dictionaries aqui também)

        if (dadosCarregados == null) return; // Segurança

        // Converte a Lista (salvável) de volta para nosso Dictionary (rápido)
        foreach (var item in dadosCarregados)
        {
             if (!string.IsNullOrEmpty(item.npcID)) // Segurança extra
             {
                 npcWorldStates[item.npcID] = item.state;
             }
        }
        // (Adicionar conversão de outras Listas para Dictionaries aqui no futuro)

        Debug.Log("WorldStateManager carregou " + npcWorldStates.Count + " estados de NPC.");
    }
}

// Classe auxiliar para salvar/carregar estados de NPC (precisa estar fora ou em outro arquivo)
// Certifique-se que esta struct/classe está definida (provavelmente no GameData.cs)
/*
[System.Serializable]
public class NPCStateSaveData
{
    public string npcID;
    public NPCState state; // Assume que NPCState é um enum definido em GameEnums.cs
}
*/