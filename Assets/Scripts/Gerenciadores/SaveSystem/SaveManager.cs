using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;

public class SaveManager : MonoBehaviour
{
    #region Singleton

    public static SaveManager Instance { get; private set; }
    public static event Action OnGameSaved;
    public bool IsSaving { get; private set; }
    public bool HasLoadedData { get; private set; }

    #endregion

    #region Fields

    [Header("--- SAVE ---")]
    [SerializeField] private string saveFileName = "savegame.json";
    [SerializeField] private bool enableDebugHotkeys = true;
    [SerializeField] private bool autoApplyDataOnSceneLoaded = true;

    private GameData gameData;
    private string saveFilePath;
    private Transform registeredPlayerTransform;
    private Coroutine saveRoutine;
    private Coroutine applyRoutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        gameData = new GameData();
        IsSaving = false;
        HasLoadedData = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (!enableDebugHotkeys || Keyboard.current == null)
            return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
            SaveGame();

        if (Keyboard.current.f9Key.wasPressedThisFrame)
            LoadGame();
    }

    #endregion

    #region Registration

    // Registra o player ativo para salvar posição e reaplicar dados após load.
    public void RegisterPlayer(Transform player)
    {
        if (player == null)
            return;

        registeredPlayerTransform = player;
        Debug.Log("SaveManager: Player registrado/atualizado com sucesso.");
    }

    #endregion

    #region Public API

    // Captura o estado atual em memória sem escrever arquivo. Use antes de trocar cena.
    public void CaptureRuntimeState()
    {
        if (gameData == null)
            gameData = new GameData();

        CaptureWorldState();
        CaptureQuestState();
        CaptureInventoryState();
        CapturePlayerState();
    }

    // Salva o estado atual em JSON no persistentDataPath.
    public void SaveGame()
    {
        if (IsSaving)
            return;

        if (saveRoutine != null)
            StopCoroutine(saveRoutine);

        saveRoutine = StartCoroutine(SaveGameRoutine());
    }

    // Carrega o JSON salvo e reaplica nos sistemas disponíveis.
    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("SaveManager: Nenhum arquivo de save encontrado. Começando jogo novo.");
            HasLoadedData = false;
            return;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(json) ?? new GameData();
            HasLoadedData = true;

            ApplyLoadedDataToRuntime();
            Debug.Log("SaveManager: Jogo carregado de: " + saveFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("SaveManager: Falha ao carregar save. Um novo GameData será criado. Erro: " + e.Message);
            gameData = new GameData();
            HasLoadedData = false;
        }
    }

    // Reseta apenas os dados em memória. Não apaga o arquivo salvo.
    public void ResetGameData()
    {
        gameData = new GameData();
        registeredPlayerTransform = null;
        HasLoadedData = false;
        Debug.Log("SaveManager: GameData em memória foi resetado.");
    }

    // Apaga o arquivo físico de save e limpa os dados em memória.
    public void DeleteSaveFile()
    {
        ResetGameData();

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("SaveManager: Arquivo de save apagado: " + saveFilePath);
        }
    }

    // Retorna uma cópia de referência do GameData atual para leitura controlada.
    public GameData GetCurrentGameData()
    {
        if (gameData == null)
            gameData = new GameData();

        return gameData;
    }

    #endregion

    #region Save Routine

    private IEnumerator SaveGameRoutine()
    {
        IsSaving = true;
        CaptureRuntimeState();

        string json = JsonUtility.ToJson(gameData, true);
        string tempPath = saveFilePath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);

            if (File.Exists(saveFilePath))
                File.Delete(saveFilePath);

            File.Move(tempPath, saveFilePath);
            Debug.Log("SaveManager: Jogo salvo em: " + saveFilePath);
            OnGameSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("SaveManager: Falha ao salvar jogo. Erro: " + e.Message);

            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        yield return null;
        IsSaving = false;
        saveRoutine = null;
    }

    #endregion

    #region Capture Runtime State

    // Captura estado do mundo: NPCs, itens, portas e eventos únicos.
    private void CaptureWorldState()
    {
        if (WorldStateManager.Instance == null)
            return;

        gameData.npcStates = WorldStateManager.Instance.GetSaveData();
        gameData.collectedItemIDs = WorldStateManager.Instance.GetItemSaveData();
        gameData.unlockedDoorIDs = WorldStateManager.Instance.GetDoorSaveData();
        gameData.triggeredEvents = WorldStateManager.Instance.GetTriggeredEventsSaveData();
    }

    // Captura o progresso atual das quests.
    private void CaptureQuestState()
    {
        if (QuestManager.Instance == null)
            return;

        gameData.activeQuests = QuestManager.Instance.GetActiveQuestsSaveData();
        gameData.completedQuestIDs = QuestManager.Instance.GetCompletedQuestsSaveData();
    }

    // Captura o inventário sem depender dele como DontDestroyOnLoad.
    private void CaptureInventoryState()
    {
        if (InventoryManager.Instance == null)
            return;

        gameData.inventoryItems = InventoryManager.Instance.GetSaveData();
    }

    // Captura posição do player atual.
    private void CapturePlayerState()
    {
        Transform playerTransform = GetRegisteredPlayerTransform();

        if (playerTransform == null)
        {
            Debug.LogWarning("SaveManager: Tentou salvar player, mas nenhum PlayerPersistent foi encontrado.");
            return;
        }

        gameData.playerPosX = playerTransform.position.x;
        gameData.playerPosY = playerTransform.position.y;
        gameData.playerPosZ = playerTransform.position.z;
    }

    #endregion

    #region Apply Loaded Data

    // Reaplica dados nos sistemas ativos. Também é usado após carregar uma nova cena.
    public void ApplyLoadedDataToRuntime()
    {
        if (gameData == null)
            gameData = new GameData();

        ApplyWorldState();
        ApplyQuestState();
        ApplyInventoryState();

        if (applyRoutine != null)
            StopCoroutine(applyRoutine);

        applyRoutine = StartCoroutine(ApplyPlayerPositionWhenReady());
    }

    private void ApplyWorldState()
    {
        if (WorldStateManager.Instance == null)
            return;

        WorldStateManager.Instance.LoadSaveData(gameData.npcStates);
        WorldStateManager.Instance.LoadItemSaveData(gameData.collectedItemIDs);
        WorldStateManager.Instance.LoadDoorSaveData(gameData.unlockedDoorIDs);
        WorldStateManager.Instance.LoadTriggeredEventsSaveData(gameData.triggeredEvents);
    }

    private void ApplyQuestState()
    {
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.LoadQuestData(gameData.activeQuests, gameData.completedQuestIDs);
    }

    private void ApplyInventoryState()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.LoadSaveData(gameData.inventoryItems);
    }

    private IEnumerator ApplyPlayerPositionWhenReady()
    {
        const int maxWaitFrames = 300;
        int currentFrames = 0;

        while (PlayerPersistent.Instance == null && currentFrames < maxWaitFrames)
        {
            currentFrames++;
            yield return null;
        }

        Transform targetTransform = GetRegisteredPlayerTransform();

        if (targetTransform == null)
        {
            Debug.LogWarning("SaveManager: PlayerPersistent não foi encontrado para aplicar posição do save.");
            applyRoutine = null;
            yield break;
        }

        Vector3 position = new Vector3(gameData.playerPosX, gameData.playerPosY, gameData.playerPosZ);
        TeleportPlayerSafely(targetTransform, position);

        Debug.Log("SaveManager: Player reposicionado com dados do save.");
        applyRoutine = null;
    }

    #endregion

    #region Scene Events

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoApplyDataOnSceneLoaded || !HasLoadedData)
            return;

        // Reaplica inventário/quests/mundo quando novos managers de cena aparecerem.
        StartCoroutine(ApplyLoadedDataNextFrame());
    }

    private IEnumerator ApplyLoadedDataNextFrame()
    {
        yield return null;
        ApplyLoadedDataToRuntime();
    }

    #endregion

    #region Player Helpers

    private Transform GetRegisteredPlayerTransform()
    {
        if (registeredPlayerTransform != null)
            return registeredPlayerTransform;

        if (PlayerPersistent.Instance != null)
        {
            registeredPlayerTransform = PlayerPersistent.Instance.transform;
            return registeredPlayerTransform;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            registeredPlayerTransform = playerObject.transform;
            return registeredPlayerTransform;
        }

        return null;
    }

    // Move o player sem acumular velocidade ou conflito com CharacterController.
    private void TeleportPlayerSafely(Transform targetTransform, Vector3 position)
    {
        CharacterController characterController = targetTransform.GetComponent<CharacterController>();
        PlayerControllerSystem playerController = targetTransform.GetComponent<PlayerControllerSystem>();
        Rigidbody rb = targetTransform.GetComponent<Rigidbody>();

        if (characterController != null)
            characterController.enabled = false;

        if (playerController != null)
            playerController.TeleportToPosition(position);

        targetTransform.position = position;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (characterController != null)
            characterController.enabled = true;
    }

    #endregion
}
