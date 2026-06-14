using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    private PlayerInputActions inputActions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Inicializa estado
        IsDeathScreenActive = false;
    }

    void Start()
    {
        Show(false);
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (menuButton) menuButton.onClick.AddListener(OnMenuClicked);

        // CORREÇÃO CRÍTICA: o OnEnable inicial roda ANTES do Start, quando
        // 'inputActions' ainda é nulo — então a inscrição em UI.Cancel nunca
        // acontecia e o ESC na tela de morte ficava mudo. Agora a primeira
        // inscrição é feita aqui, depois de obter a referência.
        if (InputManager.Instance != null)
        {
            inputActions = InputManager.Instance.InputActions;
            inputActions.UI.Cancel.performed -= OnCancelPressed; // evita duplicar
            inputActions.UI.Cancel.performed += OnCancelPressed;
        }
    }

    void OnEnable()
    {
        HealthSystem.OnPlayerDied += HandlePlayerDeath;

        // Reinscreve apenas em re-habilitações (na primeira vez, o Start cuida disso)
        if (inputActions != null)
        {
            inputActions.UI.Cancel.performed -= OnCancelPressed; // evita duplicar
            inputActions.UI.Cancel.performed += OnCancelPressed;
        }
    }

    void OnDisable()
    {
        HealthSystem.OnPlayerDied -= HandlePlayerDeath;
        
        if (inputActions != null)
        {
            inputActions.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void OnDestroy()
    {
        // Limpeza extra
        if (inputActions != null)
        {
            inputActions.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        // Guard Clause: Se não está ativo, não faz nada
        if (!IsDeathScreenActive) return;
        
        // Se está no menu principal, não faz nada
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        
        if (deathScreenGroup == null || deathScreenGroup.alpha < 0.9f) return;
        
        // Mantém seleção do botão (gamepad/teclado)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);
            if (!isMouseMoving && resumeButton != null && resumeButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // Só responde se a DeathScreen estiver REALMENTE ativa
        if (!IsDeathScreenActive) return;
        
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        // ESC = Seleciona Resume
        StartCoroutine(SelectButtonLater(resumeButton));
    }

    private void HandlePlayerDeath()
    {
        // Não ativa se já estiver ativa (evita duplicação)
        if (IsDeathScreenActive) return;
        
        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        // Não ativar se estiver no menu principal
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) yield break;
        
        // ✅ MARCA COMO ATIVA IMEDIATAMENTE (Player para de processar)
        IsDeathScreenActive = true;
        
        // Espera o delay visual (animação de morte, fade)
        yield return new WaitForSeconds(delayBeforeScreen);

        // ✅ DESABILITA O PLAYER MAP ANTES DE PAUSAR
        if (InputManager.Instance != null) 
        {
            InputManager.Instance.SwitchToUIMap();
        }
        
        // Pausa o jogo
        Time.timeScale = 0f; 

        // Mostra a UI
        Show(true);
        SetCursorLocked(false);

        // Seleciona o botão Resume
        StartCoroutine(SelectButtonLater(resumeButton));
    }

    void Show(bool visible)
    {
        if (!deathScreenGroup) return;
        
        if (deathScreenGroup.gameObject != this.gameObject)
        {
            deathScreenGroup.gameObject.SetActive(visible);
        }
        
        deathScreenGroup.alpha = visible ? 1f : 0f;
        deathScreenGroup.interactable = visible;
        deathScreenGroup.blocksRaycasts = visible;
    }
    public void Hide()
    {
        IsDeathScreenActive = false;
        Show(false);
    }

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
        // Limpa o estado
        IsDeathScreenActive = false;
        Time.timeScale = 1f;
        Hide();

        // ✅ DESTROI O PLAYER MORTO ANTES DE RECARREGAR
        if (PlayerPersistent.Instance != null)
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        // Recarrega a cena
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitUntil(() => op.isDone);

        // Carrega o save
        if (SaveManager.Instance != null) 
            SaveManager.Instance.LoadGame();
        
        // Volta para o Gameplay Map
        if (InputManager.Instance != null) 
            InputManager.Instance.SwitchToGameplayMap();
        
        SetCursorLocked(true);
    }

    public void OnMenuClicked()
    {
        if (!IsDeathScreenActive) return;
        
        CleanupForMainMenu();

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.ReturnToMainMenu();
        }
        else
        {
            if (PlayerPersistent.Instance != null) 
                Destroy(PlayerPersistent.Instance.gameObject);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }    
}