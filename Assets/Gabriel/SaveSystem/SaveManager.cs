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
    private Transform playerTransform;

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
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        this.gameData = new GameData();
        IsSaving = false;
    }

    public void ResetGameData()
    {
        this.gameData = new GameData();
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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            // Garante que a referência seja limpa se não encontrar o player
            playerTransform = null; 
            Debug.LogWarning("SaveManager não encontrou o Player para salvar a posição.");
        }

        // --- COLETA DE DADOS ATUALIZADA ---
        if (WorldStateManager.Instance != null)
        {
            this.gameData.npcStates = WorldStateManager.Instance.GetSaveData();
            this.gameData.collectedItemIDs = WorldStateManager.Instance.GetItemSaveData();
        }
        if (InventoryManager.Instance != null)
        {
            this.gameData.inventoryItems = InventoryManager.Instance.GetSaveData();
        }
        if (playerTransform != null)
        {
            this.gameData.playerPosX = playerTransform.position.x;
            this.gameData.playerPosY = playerTransform.position.y;
            this.gameData.playerPosZ = playerTransform.position.z;
        }
        // --- FIM DA COLETA ---

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

            // --- ENTREGA DE DADOS ATUALIZADA ---
            if (WorldStateManager.Instance != null)
            {
                WorldStateManager.Instance.LoadSaveData(this.gameData.npcStates);
                WorldStateManager.Instance.LoadItemSaveData(this.gameData.collectedItemIDs);
            }
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.LoadSaveData(this.gameData.inventoryItems);
            }
            // --- FIM DA ENTREGA ---

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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerControllerSystem pc = playerObj.GetComponent<PlayerControllerSystem>();
            if (pc != null)
            {
                Vector3 pos = new Vector3(gameData.playerPosX, gameData.playerPosY, gameData.playerPosZ);
                pc.TeleportToPosition(pos);
            }
        }
    }
}