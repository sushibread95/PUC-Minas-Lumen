using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections; 

public class MainMenu : MonoBehaviour
{
    [Header("Referências da UI")]
    public Button continueButton;
    public Button defaultSelectedButton;

    [Header("Debug")]
    public Button debugButton; 
    public string debugSceneName = "DebugScene"; 
    public string debugSpawnPointID = "debug_spawn_01"; 

    [Header("Configuração")]
    public string gameSceneName = "Vilarejo";

    private string saveFilePath;
    private GameObject persistentUI; // UI do jogo (HUD)

    void Start()
    {
        // 1. Destrava cursor (Inicial)
        UnlockCursor();

        // 2. Input para Menu (Mapa UI)
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }

        // --- A CORREÇÃO: DESFIBRILADOR DE INPUT MODULE ---
        // Isso força o EventSystem a reiniciar o módulo de input,
        // garantindo que ele reconheça os botões da nova cena ao voltar do jogo.
        if (EventSystem.current != null)
        {
            var inputModule = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule != null)
            {
                inputModule.enabled = false;
                inputModule.enabled = true;
                // Debug.Log("MainMenu: Input Module reiniciado com sucesso.");
            }
        }
        // --------------------------------------------------

        // 3. LIMPEZA DE MANAGERS
        if (PauseMenuManager.Instance != null) PauseMenuManager.Instance.ForceHide();
        if (DeathScreenManager.Instance != null) DeathScreenManager.Instance.Hide();
        if (CharacterMenuWindow.Instance != null) CharacterMenuWindow.Instance.CloseMenu();

        // 4. Esconde HUD do Jogo (Canvas Principal da cena Boot)
        persistentUI = GameObject.Find("Canvas Principal");
        if (persistentUI != null) persistentUI.SetActive(false);
        
        // 5. Verifica Save
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (continueButton != null) continueButton.interactable = File.Exists(saveFilePath);

        // Debug Button
        if (debugButton != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugButton.gameObject.SetActive(true);
            debugButton.onClick.AddListener(EnterDebugRoom);
#else
            debugButton.gameObject.SetActive(false);
#endif
        }

        // 6. Seleção Única
        StartCoroutine(SelectButtonLater());
    }

    // --- FORÇA BRUTA NO UPDATE (Mantido para segurança do cursor) ---
    void Update()
    {
        // Garante a cada frame que o cursor está visível e solto.
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            UnlockCursor();
        }
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    // ---------------------------------------------

    private IEnumerator SelectButtonLater()
    {
        yield return null; 

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); 

            if (defaultSelectedButton != null && defaultSelectedButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
            }
            else
            {
                Button firstButton = GetComponentInChildren<Button>();
                if (firstButton != null) 
                    EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }
    }

    public void StartNewGame()
    {
        PrepareToExitMenu();

        if (WorldStateManager.Instance != null) WorldStateManager.Instance.ResetState();
        if (SaveManager.Instance != null) SaveManager.Instance.ResetGameData();
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(gameSceneName, "fase1_spawn");
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        PrepareToExitMenu();

        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();

        SceneManager.LoadScene(gameSceneName);
    }

    public void EnterDebugRoom()
    {
        PrepareToExitMenu();

        if (WorldStateManager.Instance != null) WorldStateManager.Instance.ResetState();
        if (SaveManager.Instance != null) SaveManager.Instance.ResetGameData();
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(debugSceneName, debugSpawnPointID);
        else
            SceneManager.LoadScene(debugSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Saindo...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PrepareToExitMenu()
    {
        // Devolve o HUD antes de carregar o jogo
        if (persistentUI != null) persistentUI.SetActive(true);
    }
}