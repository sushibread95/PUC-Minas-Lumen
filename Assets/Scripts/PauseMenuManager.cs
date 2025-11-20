using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup pausePanel;
    public Button resumeButton;
    public Button saveButton;
    public Button mainMenuButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Settings")]
    public bool lockCursorInGameplay = true;
    public bool selectFirstButtonOnOpen = true;
    public string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }
    private GameObject lastSelectedGameObject;

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
        
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(SaveGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (restartButton) restartButton.onClick.AddListener(RestartScene);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
    }

    void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Pause.performed += OnPausePerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed += OnCancelPressed;
        }
        
        Show(false);
        EnsureTimescale(1f);
        
        // No Start, assumimos que o jogo começou, então travamos o cursor
        SetCursorLocked(true);
        IsPaused = false;
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
        {
            InputManager.Instance.InputActions.Player.Pause.performed -= OnPausePerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        // Lógica de manter seleção do controle no menu de pause
        if (!IsPaused || !pausePanel || pausePanel.alpha < 0.9f) return;
        
        if (EventSystem.current != null)
        {
             if (EventSystem.current.currentSelectedGameObject == null)
             {
                 bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);
                 if (!isMouseMoving) 
                 {
                     if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy)
                     {
                         EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                     }
                     else if (resumeButton != null && resumeButton.interactable)
                     {
                         EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                     }
                 }
             }
             else
             {
                 lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
             }
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // Se apertar 'Esc' ou 'B' no controle e não houver outro menu aberto (Inventário), resume o jogo
        if (IsPaused && (InventoryController.Instance == null || !InventoryController.Instance.IsInventoryOpen))
        {
            Resume();
        }
    }
    
    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsSaving) return;
        if (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy) return;
        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen) return;

        if (!IsPaused) Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Show(true);
        EnsureTimescale(0f);
        SetCursorLocked(false); // Solta o cursor para o menu
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        
        if (selectFirstButtonOnOpen && resumeButton)
        {
            StartCoroutine(SelectButtonLater(resumeButton));
        }
    }

    public void Resume()
    {
        if (!IsPaused) return;
        
        // Faz a limpeza para voltar ao jogo
        ResumeCleanup();
    }

    // Função auxiliar para "Voltar ao Jogo"
    private void ResumeCleanup()
    {
         // Reseta o estado do mundo se necessário (opcional)
         // if (WorldStateManager.Instance != null) WorldStateManager.Instance.ResetState(); // <-- CUIDADO: ISSO RESETARIA TUDO AO DESPAUSAR. REMOVIDO.
         
         if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap(); 
         EnsureTimescale(1f); 
         SetCursorLocked(lockCursorInGameplay); // Trava o cursor de novo
         IsPaused = false; 
         if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); 
         lastSelectedGameObject = null;
         Show(false); 
    }

    public void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    // --- CORREÇÃO PRINCIPAL AQUI ---
    public void QuitToMainMenu()
    {
        // NÃO chamamos ResumeCleanup() aqui, pois ele travaria o cursor.
        // Em vez disso, fazemos uma limpeza manual para MENU.
        
        EnsureTimescale(1f); // O tempo volta ao normal
        IsPaused = false;
        Show(false); // Esconde o painel de pause

        // Carrega o Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
    // ------------------------------

    public void RestartScene()
    {
        ResumeCleanup(); // Aqui tudo bem limpar para gameplay, pois vamos recarregar a fase
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void QuitGame()
    {
        EnsureTimescale(1f);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
    
    void Show(bool visible)
    {
        if (!pausePanel) return;
        pausePanel.gameObject.SetActive(visible);
        pausePanel.alpha = visible ? 1f : 0f;
        pausePanel.interactable = visible;
        pausePanel.blocksRaycasts = visible;
    }

    void EnsureTimescale(float value)
    {
        Time.timeScale = value;
        AudioListener.pause = (value == 0f);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private IEnumerator SelectButtonLater(Button button)
    {
        yield return null;
        if (button != null && EventSystem.current != null && button.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}