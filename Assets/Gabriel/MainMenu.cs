using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [Header("Referências da UI")]
    public Button continueButton;
    public Button defaultSelectedButton;

    // --- ADIÇÃO: Botão de Debug ---
    [Header("Debug (Editor Only)")]
    public Button debugButton; // Arraste seu botão de Debug aqui
    public string debugSceneName = "DebugScene"; // Nome da cena de testes
    public string debugSpawnPointID = "debug_spawn_01"; // ID do SceneEntrance na cena de testes

    [Header("Nomes das Cenas")]
    public string gameSceneName = "Vilarejo";

    private string saveFilePath;
    private GameObject lastSelectedGameObject;

    void Start()
    {
        // ... (Lógica de Cursor e Input existente) ...
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }
        else
        {
            Debug.LogWarning("[MainMenu] InputManager.Instance não encontrado.");
        }

        // Lógica do Save
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(saveFilePath);
        }

        // --- LÓGICA DO BOTÃO DEBUG ---
        if (debugButton != null)
        {
            // O botão só aparece se estivermos no Editor ou numa Build de Desenvolvimento
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugButton.gameObject.SetActive(true);
            debugButton.onClick.AddListener(EnterDebugRoom);
#else
                debugButton.gameObject.SetActive(false);
#endif
        }

        SelectDefaultButton();
    }

    // ... (Update e SelectDefaultButton permanecem iguais) ...
    void Update()
    {
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);
            if (!isMouseMoving)
            {
                if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy && lastSelectedGameObject.GetComponent<Selectable>()?.IsInteractable() == true)
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                else
                    SelectDefaultButton();
            }
        }
        else if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }
    }

    private void SelectDefaultButton()
    {
        if (EventSystem.current == null) return;
        if (defaultSelectedButton != null && defaultSelectedButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
            lastSelectedGameObject = defaultSelectedButton.gameObject;
        }
        else
        {
            Button firstInteractableButton = GetComponentInChildren<Button>(false);
            if (firstInteractableButton != null && firstInteractableButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(firstInteractableButton.gameObject);
                lastSelectedGameObject = firstInteractableButton.gameObject;
            }
        }
    }

    public void StartNewGame()
    {
        // ... (Seu código original de StartNewGame) ...
        if (WorldStateManager.Instance != null) WorldStateManager.Instance.ResetState();
        if (SaveManager.Instance != null) SaveManager.Instance.ResetGameData();

        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();

        Debug.Log("INICIANDO NOVO JOGO...");

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(gameSceneName, "fase1_spawn");
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        // ... (Seu código original de ContinueGame) ...
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();

        Debug.Log("CONTINUANDO JOGO...");

        if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();

        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        // ... (Seu código original de QuitGame) ...
        Debug.Log("Fechando o jogo...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- NOVA FUNÇÃO DE DEBUG ---
    public void EnterDebugRoom()
    {
        Debug.LogWarning("ENTRANDO NA SALA DE DEBUG...");

        // 1. Limpa o estado (importante para testar mecânicas sem sujeira de saves antigos)
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.ResetState();
        }

        // 2. Reseta dados do SaveManager (opcional, mas recomendado para testes limpos)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetGameData();
        }

        // 3. ESSENCIAL: Muda o Input para Gameplay
        // Sem isso, o boneco nasce mas os controles não funcionam
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }

        // 4. Carrega a cena de Debug no spawn específico
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.TransitionToScene(debugSceneName, debugSpawnPointID);
        }
        else
        {
            // Fallback caso não tenha TransitionManager
            SceneManager.LoadScene(debugSceneName);
        }
    }
}