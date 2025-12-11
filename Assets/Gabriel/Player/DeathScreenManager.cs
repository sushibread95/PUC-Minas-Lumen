using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance;
    public bool IsDeathScreenActive { get; private set; }

    [Header("UI References")]
    public CanvasGroup deathScreenGroup; 
    public Button resumeButton;         
    public Button menuButton;           
    
    [Header("Settings")]
    public float delayBeforeScreen = 2.0f;
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        Show(false);
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (menuButton) menuButton.onClick.AddListener(OnMenuClicked);
        
        // Inscrever para eventos de input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.InputActions.UI.Cancel.performed += OnCancelPressed;
        }
    }

    void OnDestroy()
    {
        // Limpar eventos
        if (InputManager.Instance != null && InputManager.Instance.InputActions != null)
        {
            InputManager.Instance.InputActions.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        // Se estivermos no menu principal, não fazer nada.
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (!IsDeathScreenActive) return;
        
        if (deathScreenGroup == null || deathScreenGroup.alpha < 0.9f) return;
        
        // Manter seleção do botão (similar ao PauseMenuManager)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);
            if (!isMouseMoving && resumeButton != null && resumeButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
        }
    }

    void OnEnable()
    {
        HealthSystem.OnPlayerDied += HandlePlayerDeath;
    }

    void OnDisable()
    {
        HealthSystem.OnPlayerDied -= HandlePlayerDeath;
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // Se estivermos no menu principal, não fazer nada.
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (IsDeathScreenActive)
        {
            StartCoroutine(SelectButtonLater(resumeButton));
        }
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        // Não ativar se estiver no menu principal
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) yield break;
        
        yield return new WaitForSeconds(delayBeforeScreen);

        Time.timeScale = 0f; 
        IsDeathScreenActive = true;

        // Mostrar UI primeiro
        Show(true);

        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        SetCursorLocked(false);

        StartCoroutine(SelectButtonLater(resumeButton));
    }

    void Show(bool visible)
    {
        if (!deathScreenGroup) return;
        deathScreenGroup.alpha = visible ? 1f : 0f;
        deathScreenGroup.interactable = visible;
        deathScreenGroup.blocksRaycasts = visible;
    }

    public void Hide()
    {
        IsDeathScreenActive = false;
        Show(false);
    }

    // Método de limpeza para quando for para o menu principal
    public void CleanupForMainMenu()
    {
        IsDeathScreenActive = false;
        Time.timeScale = 1f;
        Show(false);
        SetCursorLocked(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
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

    public void OnResumeClicked()
    {
        if (!IsDeathScreenActive) return;
        StartCoroutine(ReloadAndLoadSave());
    }

    private IEnumerator ReloadAndLoadSave()
    {
        IsDeathScreenActive = false;
        Time.timeScale = 1f;
        Hide();

        AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitUntil(() => op.isDone);

        if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();
        
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        SetCursorLocked(true);
    }

    public void OnMenuClicked()
    {
        if (!IsDeathScreenActive) return;
        
        // Chama a limpeza para o menu principal
        CleanupForMainMenu();

        // Usar TransitionManager se disponível (similar ao PauseMenuManager)
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.ReturnToMainMenu();
        }
        else
        {
            if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }    
}