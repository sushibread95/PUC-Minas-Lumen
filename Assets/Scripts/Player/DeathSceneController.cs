using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.IO; // Necessário para checar se o arquivo de save existe

public class DeathSceneController : MonoBehaviour
{
    [Header("Botões")]
    [Tooltip("Este botão agora funciona como 'Continue' (Carregar Save)")]
    public Button continueButton; 
    public Button mainMenuButton;

    [Header("Configurações de Cena")]
    [Tooltip("Nome EXATO da cena de jogo onde o save foi feito (ex: Vilarejo)")]
    public string gameplaySceneName = "Vilarejo"; 
    
    [Tooltip("Spawn inicial caso NÃO exista save game ainda (Fallback)")]
    public string defaultSpawnPointID = "start_spawn"; 

    [Tooltip("Nome da cena do Menu Principal")]
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        // 1. Destrava Cursor e Arruma TimeScale
        UnlockCursor();
        Time.timeScale = 1f;

        // 2. Força Input para UI
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();

        // 3. O "Desfibrilador" do EventSystem (Para os cliques funcionarem)
        if (EventSystem.current != null)
        {
            var inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                inputModule.enabled = false;
                inputModule.enabled = true;
            }
        }

        // 4. Limpeza Preventiva
        if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);
        if (PauseMenuManager.Instance != null) PauseMenuManager.Instance.ForceHide();

        // 5. Setup dos Botões
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnMenuClicked);
    }

    void Update()
    {
        // Garante cursor solto sempre
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

    // --- A NOVA LÓGICA DE CONTINUE ---
    void OnContinueClicked()
    {
        // 1. Reseta Status básicos
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2. Destrói o Player Antigo (CRUCIAL)
        // O SaveManager vai posicionar o novo player ou o PlayerPersistent vai renascer no lugar certo
        if (PlayerPersistent.Instance != null) 
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        // 3. Verifica se existe Save
        string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        bool saveExists = File.Exists(savePath);

        if (saveExists && SaveManager.Instance != null)
        {
            Debug.Log("DeathScene: Save encontrado. Carregando jogo...");
            
            // Carrega os dados do Save (Inventário, Posição, Quests)
            SaveManager.Instance.LoadGame();
            
            // Carrega a cena do jogo (O SaveManager vai teleportar o player depois que a cena abrir)
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.TransitionToScene(gameplaySceneName, null); // Null porque a posição vem do Save
            else
                SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.Log("DeathScene: Nenhum save encontrado. Reiniciando fase do zero.");
            
            // Se não tem save, faz um Restart normal no Spawn Point padrão
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.TransitionToScene(gameplaySceneName, defaultSpawnPointID);
            else
                SceneManager.LoadScene(gameplaySceneName);
        }
    }

    void OnMenuClicked()
    {
        Time.timeScale = 1f;
        if (PlayerPersistent.Instance != null) Destroy(PlayerPersistent.Instance.gameObject);

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.ReturnToMainMenu();
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}