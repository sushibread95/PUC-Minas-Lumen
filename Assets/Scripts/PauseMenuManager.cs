// Nome do arquivo: PauseMenuManager.cs
// CÓDIGO COMPLETO (COM LÓGICA DE PERSISTÊNCIA)

using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro; 
using System.Collections.Generic;
using System.Linq; 

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    // ... (variáveis e cabeçalhos permanecem iguais) ...
    public CanvasGroup pausePanel;
    public Button resumeButton;
    public Button saveButton;
    public Button mainMenuButton;
    public Button restartButton;
    public Button quitButton;
    public bool lockCursorInGameplay = true;
    public bool selectFirstButtonOnOpen = true;
    public string mainMenuSceneName = "MainMenu";
    public bool IsPaused { get; private set; }
    private GameObject lastSelectedGameObject;

    void Awake()
    {
        // --- CORREÇÃO: ADICIONANDO PERSISTÊNCIA ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // ESSENCIAL: Mantém o Menu ativo entre as cenas.
            DontDestroyOnLoad(gameObject); 
        }
        // --- FIM DA CORREÇÃO ---
        
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(SaveGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (restartButton) restartButton.onClick.AddListener(RestartScene);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
    }
    
    // ... (restante do código OnDestroy, Update, Pause/Resume, etc. permanece igual) ...
    // ... (Para economizar espaço, o restante do script é omitido) ...

    void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.Player.Pause.performed += OnPausePerformed;
            InputManager.Instance.InputActions.UI.Cancel.performed += OnCancelPressed;
        }
        else Debug.LogError("PauseMenuManager não conseguiu encontrar o InputManager.");
        
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
            InputManager.Instance.InputActions.Player.Pause.performed -= OnCancelPressed; // Correção de segurança
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
                         else lastSelectedGameObject = null;
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
        if (SaveManager.Instance != null && SaveManager.Instance.IsSaving) return;
        if (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy) return;
        
        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
        {
            return;
        }

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
            lastSelectedGameObject = resumeButton.gameObject;
        }
        else
        {
             lastSelectedGameObject = null;
             if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
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