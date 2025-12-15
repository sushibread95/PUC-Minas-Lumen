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
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    void Start()
    {
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(SaveGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (restartButton) restartButton.onClick.AddListener(RestartScene);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Pause.performed += OnPausePerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed += OnCancelPressed;
        }
        
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

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        // Se o player estiver morto, o Pause Manager ignora o ESC
        // (Isso evita pausar durante a animação de morte)
        if (HealthSystem.Instance != null && HealthSystem.Instance.isDead) return;

        if (IsPaused && (InventoryController.Instance == null || !InventoryController.Instance.IsInventoryOpen))
        {
            Resume();
        }
    }
    
    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        if (HealthSystem.Instance != null && HealthSystem.Instance.isDead) return;

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

    public void CleanupForMainMenu()
    {
        IsPaused = false;
        EnsureTimescale(1f);
        Show(false);
        SetCursorLocked(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        lastSelectedGameObject = null;
    }

    public void ForceHide() { CleanupForMainMenu(); }

    public void SaveGame() { if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); }

    public void QuitToMainMenu()
    {
        CleanupForMainMenu();
        if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartScene()
    {
        ResumeCleanup(); 
        if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame() { Application.Quit(); }
    
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
        yield return new WaitForSecondsRealtime(0.1f);
        if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}