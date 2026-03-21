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
        
        // 1. Salva estado do mundo (NPCs, Itens, Portas e EVENTOS)
        if (WorldStateManager.Instance != null)
        {
            this.gameData.npcStates = WorldStateManager.Instance.GetSaveData();
            this.gameData.collectedItemIDs = WorldStateManager.Instance.GetItemSaveData();
            this.gameData.unlockedDoorIDs = WorldStateManager.Instance.GetDoorSaveData();
            
            // --- ADIÇÃO: Salva os eventos já triggados (Diálogos únicos) ---
            this.gameData.triggeredEvents = WorldStateManager.Instance.GetTriggeredEventsSaveData();
            // --------------------------------------------------------------
        }

        // 2. Salva Quests
        if (QuestManager.Instance != null)
        {
            this.gameData.activeQuests = QuestManager.Instance.GetActiveQuestsSaveData();
            this.gameData.completedQuestIDs = QuestManager.Instance.GetCompletedQuestsSaveData();
        }

        // 3. Salva Inventário
        if (InventoryManager.Instance != null)
        {
            this.gameData.inventoryItems = InventoryManager.Instance.GetSaveData();
        }

        // 4. Salva Posição do Player
        if (registeredPlayerTransform != null)
        {
            this.gameData.playerPosX = registeredPlayerTransform.position.x;
            this.gameData.playerPosY = registeredPlayerTransform.position.y;
            this.gameData.playerPosZ = registeredPlayerTransform.position.z;
        }
        else
        {
            // Tenta recuperar referência caso perdida (Persistência)
            if (PlayerPersistent.Instance != null)
            {
                registeredPlayerTransform = PlayerPersistent.Instance.transform;
                this.gameData.playerPosX = registeredPlayerTransform.position.x;
                this.gameData.playerPosY = registeredPlayerTransform.position.y;
                this.gameData.playerPosZ = registeredPlayerTransform.position.z;
            }
            else
            {
                Debug.LogWarning("SaveManager: Tentou salvar, mas nenhum Player está registrado.");
            }
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

            // 1. Carrega Mundo (NPCs, Itens, Portas e EVENTOS)
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.LoadSaveData(this.gameData.npcStates);
                WorldStateManager.Instance.LoadItemSaveData(this.gameData.collectedItemIDs);
                WorldStateManager.Instance.LoadDoorSaveData(this.gameData.unlockedDoorIDs);
                
                // --- ADIÇÃO: Carrega eventos já triggados ---
                WorldStateManager.Instance.LoadTriggeredEventsSaveData(this.gameData.triggeredEvents);
                // --------------------------------------------
            }
            
            // 2. Carrega Quests
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.LoadQuestData(this.gameData.activeQuests, this.gameData.completedQuestIDs);
            }

            // 3. Carrega Inventário
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
    // Em vez de esperar 1 frame (yield return null), nós dizemos para a Unity:
    // "Fique em loop aguardando ATÉ que o PlayerPersistent apareça na cena!"
    // Limitamos a umas tentativas para não criar um loop infinito se algo der muito errado.
    
    int maxWaitFrames = 300; // Limite de 5 segundos a 60fps
    int currentFrames = 0;

    while (PlayerPersistent.Instance == null && currentFrames < maxWaitFrames)
    {
        currentFrames++;
        yield return null;
    }

    Transform targetTransform = registeredPlayerTransform;

    if (targetTransform == null && PlayerPersistent.Instance != null)
    {
        targetTransform = PlayerPersistent.Instance.transform;
        RegisterPlayer(targetTransform); 
    }

    if (targetTransform != null)
    {
        var pc = targetTransform.GetComponent<PlayerControllerSystem>();
        var cc = targetTransform.GetComponent<CharacterController>();

        if (pc != null)
        {
            Vector3 pos = new Vector3(gameData.playerPosX, gameData.playerPosY, gameData.playerPosZ);
            
            // O seu teletransporte brilhante
            if (cc) cc.enabled = false;
            pc.TeleportToPosition(pos); 
            targetTransform.position = pos; 
            if (cc) cc.enabled = true;
            
            Debug.Log("SaveManager: Player teleportado com sucesso para a posição do save!");
        }
    }
    else
    {
        Debug.LogWarning("SaveManager: Falha crítica. Player Persistente não foi encontrado mesmo após aguardar.");
    }
}
}