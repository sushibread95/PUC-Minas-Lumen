using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using System;
using System.Collections; // <-- NECESSÁRIO PARA COROUTINE

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static event Action OnGameSaved;

    // --- A "TRAVA" DE SALVAMENTO ---
    public bool IsSaving { get; private set; }
    // ---------------------------------

    private GameData gameData;
    private string saveFilePath;

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
        IsSaving = false; // Garante que a trava começa desligada
    }

    void Start()
    {
        // (LoadGame() é chamado pelo MainMenu)
    }

    void Update()
    {
        // Teclas de debug (F5/F9)
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveGame();
        }
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LoadGame();
        }
    }

    // A FUNÇÃO PÚBLICA CHAMA A "TRAVA"
    public void SaveGame()
    {
        if (IsSaving)
        {
            Debug.LogWarning("Tentativa de salvar enquanto já estava salvando. Ignorado.");
            return;
        }
        StartCoroutine(SaveGameRoutine());
    }

    // O PROCESSO DE SALVAR É UMA COROUTINE
    private IEnumerator SaveGameRoutine()
    {
        IsSaving = true;
        Debug.Log("SALVANDO JOGO...");

        // Coleta os dados
        this.gameData.npcStates = WorldStateManager.Instance.GetSaveData();
        this.gameData.inventoryItems = InventoryManager.Instance.GetSaveData();

        // Converte para JSON
        string json = JsonUtility.ToJson(this.gameData, true); 

        // Escreve no disco
        File.WriteAllText(saveFilePath, json);
        
        yield return null; // Espera um frame

        Debug.Log("JOGO SALVO EM: " + saveFilePath);

        OnGameSaved?.Invoke(); // Dispara o evento (avisa a UI)

        yield return new WaitForSecondsRealtime(1f); // Espera 1s antes de destravar

        IsSaving = false;
        Debug.Log("Trava de salvamento liberada.");
    }


    // --- FUNÇÃO CORRIGIDA ---
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("CARREGANDO JOGO...");

            // 1. Lê o arquivo
            string json = File.ReadAllText(saveFilePath);
            
            // 2. Converte o json (DENTRO DO IF)
            this.gameData = JsonUtility.FromJson<GameData>(json); 

            // 3. Entrega os dados
            WorldStateManager.Instance.LoadSaveData(this.gameData.npcStates);
            InventoryManager.Instance.LoadSaveData(this.gameData.inventoryItems);

            Debug.Log("JOGO CARREGADO!");
        }
        else
        {
            Debug.Log("Nenhum arquivo de save encontrado. Começando jogo novo.");
        }
    }
    // --- FIM DA CORREÇÃO ---
}