using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static event Action OnGameSaved;
    public bool IsSaving { get; private set; }

    private GameData gameData;
    private string saveFilePath;
    private Transform registeredPlayerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        this.gameData = new GameData();
        IsSaving = false;
    }
    
    public void RegisterPlayer(Transform player)
    {
        if (registeredPlayerTransform == null)
        {
            Debug.Log("SaveManager: Player foi registrado com sucesso.");
            registeredPlayerTransform = player;
        }
    }

    public void ResetGameData()
    {
        this.gameData = new GameData();
        registeredPlayerTransform = null; 
        Debug.Log("GameData (no SaveManager) foi resetado.");
    }

    void Start() { /* LoadGame é chamado pelo MainMenu */ }

    void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame) SaveGame();
        if (Keyboard.current.f9Key.wasPressedThisFrame) LoadGame();
    }

    public void SaveGame()
    {
        if (IsSaving) return;
        StartCoroutine(SaveGameRoutine());
    }

    private IEnumerator SaveGameRoutine()
    {
        IsSaving = true;
        Debug.Log("SALVANDO JOGO...");
        
        if (WorldStateManager.Instance != null)
        {
            this.gameData.npcStates = WorldStateManager.Instance.GetSaveData();
            this.gameData.collectedItemIDs = WorldStateManager.Instance.GetItemSaveData();
            this.gameData.unlockedDoorIDs = WorldStateManager.Instance.GetDoorSaveData();
        }

        // --- MODIFICAÇÃO: Salva Quests no GameData ---
        if (QuestManager.Instance != null)
        {
            this.gameData.activeQuests = QuestManager.Instance.GetActiveQuestsSaveData();
            this.gameData.completedQuestIDs = QuestManager.Instance.GetCompletedQuestsSaveData();
        }
        // ---------------------------------------------

        // --- AVISO DE CORREÇÃO---
        if (InventoryManager.Instance != null)
        {
            this.gameData.inventoryItems = InventoryManager.Instance.GetSaveData();
        }
        if (registeredPlayerTransform != null)
        {
            this.gameData.playerPosX = registeredPlayerTransform.position.x;
            this.gameData.playerPosY = registeredPlayerTransform.position.y;
            this.gameData.playerPosZ = registeredPlayerTransform.position.z;
        }
        else
        {
            Debug.LogWarning("SaveManager: Tentou salvar, mas nenhum Player está registrado.");
        }

        string json = JsonUtility.ToJson(this.gameData, true); 
        File.WriteAllText(saveFilePath, json);
        yield return null; 
        Debug.Log("JOGO SALVO EM: " + saveFilePath);
        OnGameSaved?.Invoke();
        yield return new WaitForSecondsRealtime(1f); 
        IsSaving = false;
        Debug.Log("Trava de salvamento liberada.");
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("CARREGANDO JOGO...");
            string json = File.ReadAllText(saveFilePath);
            this.gameData = JsonUtility.FromJson<GameData>(json);

            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.LoadSaveData(this.gameData.npcStates);
                WorldStateManager.Instance.LoadItemSaveData(this.gameData.collectedItemIDs);
                WorldStateManager.Instance.LoadDoorSaveData(this.gameData.unlockedDoorIDs);
            }
            
            // --- MODIFICAÇÃO: Carrega Quests ---
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.LoadQuestData(this.gameData.activeQuests, this.gameData.completedQuestIDs);
            }
            // -----------------------------------

            // --- AVISO DE CORREÇÃO ---
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.LoadSaveData(this.gameData.inventoryItems);
            }

            StartCoroutine(TeleportPlayerAfterSceneLoad());
            Debug.Log("JOGO CARREGADO!");
        }
        else
        {
            Debug.Log("Nenhum arquivo de save encontrado. Começando jogo novo.");
        }
    }
    
    private IEnumerator TeleportPlayerAfterSceneLoad()
    {
        yield return null; 

        float timeout = 5f; 
        while (registeredPlayerTransform == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (registeredPlayerTransform != null)
        {
            // --- PEQUENO AJUSTE: Use 'PlayerControllerSystem' ou o nome exato do seu script aqui ---
            // Como não tenho o script PlayerController, mantive como estava, assumindo que você tem 'PlayerControllerSystem'
            var pc = registeredPlayerTransform.GetComponent<PlayerControllerSystem>();
            if (pc != null)
            {
                Vector3 pos = new Vector3(gameData.playerPosX, gameData.playerPosY, gameData.playerPosZ);
                pc.TeleportToPosition(pos);
            }
        }
        else
        {
            Debug.LogError("SaveManager (Teleport): Não foi possível teleportar o Player. Nenhum player se registrou!");
        }
    }
}