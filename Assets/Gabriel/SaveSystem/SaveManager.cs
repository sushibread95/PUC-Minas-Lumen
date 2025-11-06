// Nome do arquivo: SaveManager.cs
// CÓDIGO COMPLETO E LIMPO (COM LÓGICA DE REGISTRO E ESPERA)

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
    
    // A referência que o Player vai preencher
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
    
    // O PlayerController chama esta função no Start() dele.
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
        registeredPlayerTransform = null; // Limpa o player registrado
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
        
        // --- LÓGICA DE SALVAMENTO (USANDO O REGISTRO) ---
        if (WorldStateManager.Instance != null)
        {
            this.gameData.npcStates = WorldStateManager.Instance.GetSaveData();
            this.gameData.collectedItemIDs = WorldStateManager.Instance.GetItemSaveData();
        }
        if (InventoryManager.Instance != null)
        {
            this.gameData.inventoryItems = InventoryManager.Instance.GetSaveData();
        }
        
        // Usamos a referência registrada
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
        // --- FIM DA LÓGICA DE SALVAMENTO ---

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
            }
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.LoadSaveData(this.gameData.inventoryItems);
            }

            // --- ESTA É A CORREÇÃO DE TIMING ---
            // A rotina de teleporte é iniciada AQUI, pelo próprio SaveManager,
            // assim que ele termina de carregar os dados.
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
        // 1. Espera um frame para a cena começar a carregar
        yield return null; 

        // 2. --- CORREÇÃO DE TIMING ---
        // Agora, esperamos ativamente (em loop) até que o Player
        // chame 'RegisterPlayer' e preencha a variável.
        float timeout = 5f; // (5 segundos de segurança)
        while (registeredPlayerTransform == null && timeout > 0f)
        {
            yield return null; // Espera o próximo frame
            timeout -= Time.deltaTime;
        }
        // --- FIM DA CORREÇÃO ---

        // 3. Agora, executamos o teleporte
        if (registeredPlayerTransform != null)
        {
            PlayerControllerSystem pc = registeredPlayerTransform.GetComponent<PlayerControllerSystem>();
            if (pc != null)
            {
                Vector3 pos = new Vector3(gameData.playerPosX, gameData.playerPosY, gameData.playerPosZ);
                pc.TeleportToPosition(pos);
            }
        }
        else
        {
            // Se o log de erro "Nenhum player se registrou" aparecer AGORA,
            // significa que o PlayerControllerSystem.Start() nunca rodou.
            Debug.LogError("SaveManager (Teleport): Não foi possível teleportar o Player. Nenhum player se registrou!");
        }
    }
}