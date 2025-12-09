using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Necessário para selecionar botões
using TMPro;
using System.Collections;

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance;

    [Header("UI References")]
    public CanvasGroup deathScreenGroup; // --- MUDANÇA: Usando CanvasGroup para controle total ---
    public Button resumeButton;         
    public Button menuButton;           
    public TextMeshProUGUI deathText;   
    
    [Header("Settings")]
    public float delayBeforeScreen = 2.0f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // Garante que comece escondido e não clicável
        Show(false);
        
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (menuButton) menuButton.onClick.AddListener(OnMenuClicked);
    }

    void OnEnable()
    {
        HealthSystem.OnPlayerDied += HandlePlayerDeath;
    }

    void OnDisable()
    {
        HealthSystem.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        // Espera o drama da morte
        yield return new WaitForSeconds(delayBeforeScreen);

        // --- ATIVAÇÃO ROBUSTA ---
        // 1. Mostra a tela e habilita interações
        Show(true);

        // 2. Destrava o cursor (igual ao PauseManager)
        SetCursorLocked(false);

        // 3. Troca o Input para UI (para o player não andar no fundo)
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();

        // 4. Seleciona o primeiro botão automaticamente (Vital para teclado/controle)
        StartCoroutine(SelectButtonLater(resumeButton));
    }

    // Função auxiliar para controlar visibilidade e interação (Baseada no PauseMenuManager)
    void Show(bool visible)
    {
        if (!deathScreenGroup) return;
        
        deathScreenGroup.alpha = visible ? 1f : 0f; // Transparência
        deathScreenGroup.interactable = visible;    // Pode clicar?
        deathScreenGroup.blocksRaycasts = visible;  // Bloqueia cliques atrás?
        
    }

    // Função auxiliar de cursor (Baseada no PauseMenuManager)
    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Corrotina para garantir o foco no botão (Baseada no PauseMenuManager)
    private IEnumerator SelectButtonLater(Button button)
    {
        yield return null; // Espera um frame para a UI "existir" pro sistema
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); // Limpa seleção anterior
            EventSystem.current.SetSelectedGameObject(button.gameObject); // Seleciona o novo
        }
    }

    public void OnResumeClicked()
    {
        StartCoroutine(ReloadAndLoadSave());
    }

    private IEnumerator ReloadAndLoadSave()
    {
        // Garante que o tempo esteja normal antes de carregar
        Time.timeScale = 1f;

        // Esconde a tela para não ficar piscando durante o load
        Show(false); 

        // 1. Recarrega a cena
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitUntil(() => op.isDone);

        // 2. Carrega o Save
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
        
        // 3. Restaura inputs e trava cursor
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        SetCursorLocked(true);
    }

public void Hide()
    {
        Show(false);
    }

    public void OnMenuClicked()
    {
        Time.timeScale = 1f; 

        // --- ADIÇÃO: Esconde a tela antes de sair ---
        Show(false); 
        // ------------------------------------------

        if (PlayerPersistent.Instance != null)
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }    


}