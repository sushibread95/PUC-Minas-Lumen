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
        
        // Garante estado limpo no início
        CleanupForMainMenu(); 
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
        // Se não estiver pausado, ou se estivermos na cena do Menu Principal, não rode lógica de seleção.
        if (!IsPaused || SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        
        if (pausePanel == null || pausePanel.alpha < 0.9f) return;
        
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
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (IsPaused && (InventoryController.Instance == null || !InventoryController.Instance.IsInventoryOpen))
        {
            Resume();
        }
    }
    
    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (SaveManager.Instance != null && SaveManager.Instance.IsSaving) return;
        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen) return;

        if (!IsPaused) Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Show(true);
        EnsureTimescale(0f);
        SetCursorLocked(false);
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        
        if (selectFirstButtonOnOpen && resumeButton)
        {
            StartCoroutine(SelectButtonLater(resumeButton));
        }
    }

    public void Resume()
    {
        if (!IsPaused) return;
        ResumeCleanup();
    }

    // Usado para voltar ao jogo
    public void ResumeCleanup()
    {
         if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap(); 
         EnsureTimescale(1f); 
         SetCursorLocked(lockCursorInGameplay); 
         IsPaused = false; 
         if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); 
         lastSelectedGameObject = null;
         Show(false); 
    }

    // --- NOVA FUNÇÃO DE LIMPEZA TOTAL ---
    // Usada pelo MainMenu e pelo TransitionManager
    public void CleanupForMainMenu()
    {
        IsPaused = false;
        EnsureTimescale(1f);
        Show(false); // Garante blocksRaycasts = false
        
        // Libera cursor (menu principal cuidará disso)
        SetCursorLocked(false);
        
        // Limpa seleção
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        
        lastSelectedGameObject = null;
    }

    // Mantido para compatibilidade com chamadas antigas do MainMenu.cs
    public void ForceHide()
    {
        CleanupForMainMenu();
    }
    // ------------------------------------

    public void SaveGame()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    // --- FUNÇÃO MODIFICADA ---
    public void QuitToMainMenu()
    {
        // 1. Tenta usar o TransitionManager (Jeito Correto/Persistente)
        if (TransitionManager.Instance != null)
        {
            // O TransitionManager vai cuidar de destruir o player e carregar a cena
            // Ele também pode chamar o nosso CleanupForMainMenu() internamente se configurado
            TransitionManager.Instance.ReturnToMainMenu();
            
            // Garantimos a limpeza local antes de entregar o controle
            CleanupForMainMenu();
            return;
        }
        
        // 2. Fallback Manual (Caso TransitionManager não exista)
        CleanupForMainMenu();
        
        if (PlayerPersistent.Instance != null)
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
    // -------------------------

    public void RestartScene()
    {
        ResumeCleanup();
        if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);

        var currentSceneName = SceneManager.GetActiveScene().name;
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.TransitionToScene(currentSceneName, "fase1_spawn"); 
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
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