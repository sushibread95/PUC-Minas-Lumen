using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Panel (CanvasGroup)")]
    public CanvasGroup pausePanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button saveButton;
    public Button mainMenuButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Behavior")]
    public bool lockCursorInGameplay = true;
    public bool selectFirstButtonOnOpen = true;

    [Header("Scene Names")]
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

        // Liga os botões
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
        else
        {
            Debug.LogError("PauseMenuManager não conseguiu encontrar o InputManager. O Pause não vai funcionar.");
        }

        Show(false);
        EnsureTimescale(1f);
        SetCursorLocked(true);
        IsPaused = false;
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Pause.performed -= OnPausePerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        if (!IsPaused || !pausePanel || pausePanel.alpha < 0.9f) return;

        if (EventSystem.current != null)
        {
             if (EventSystem.current.currentSelectedGameObject == null)
             {
                 bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);

                 if (!isMouseMoving) 
                 {
                     if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy && lastSelectedGameObject.GetComponent<Selectable>()?.IsInteractable() == true)
                     {
                         EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                     }
                     else if (resumeButton != null && resumeButton.IsInteractable()) 
                     {
                         EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                         lastSelectedGameObject = resumeButton.gameObject;
                     }
                      else 
                     {
                         Selectable firstInteractable = pausePanel.GetComponentInChildren<Selectable>(false);
                         if (firstInteractable != null && firstInteractable.IsInteractable())
                         {
                            EventSystem.current.SetSelectedGameObject(firstInteractable.gameObject);
                            lastSelectedGameObject = firstInteractable.gameObject;
                         }
                         else 
                         {
                            lastSelectedGameObject = null; 
                         }
                     }
                 }
             }
             else if (EventSystem.current.currentSelectedGameObject != null)
             {
                 lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
             }
        }
    }
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (IsPaused && (InventoryController.Instance == null || !InventoryController.Instance.IsInventoryOpen))
        {
            Resume();
        }
    }

    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsSaving)
        {
            Debug.LogWarning("PAUSE BLOQUEADO! O jogo está salvando.");
            return;
        }
        if (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy)
        {
            return;
        }
        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
        {
            return; 
        }

        if (!IsPaused)
        {
            Pause();
        }
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
            lastSelectedGameObject = resumeButton.gameObject;
        }
        else
        {
             lastSelectedGameObject = null;
             if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        Debug.Log("Jogo pausado - UI Input habilitado");
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Show(false);
        EnsureTimescale(1f);
        SetCursorLocked(lockCursorInGameplay);

        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        lastSelectedGameObject = null;

        Debug.Log("Jogo retomado - Gameplay Input habilitado");
    }

    private IEnumerator SelectButtonLater(Button button)
    {
        yield return null;
        if (button != null && EventSystem.current != null && button.interactable) // Garante que é interativo
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    public void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    public void QuitToMainMenu()
    {
        ResumeCleanup(); 
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartScene()
    {
        ResumeCleanup(); 
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void ResumeCleanup()
    {
         if (WorldStateManager.Instance != null)
         {
             WorldStateManager.Instance.ResetState(); 
         }
         
         if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap(); 
         EnsureTimescale(1f); 
         SetCursorLocked(lockCursorInGameplay); 
         IsPaused = false; 
         if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); 
         lastSelectedGameObject = null;
         Show(false);
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
}