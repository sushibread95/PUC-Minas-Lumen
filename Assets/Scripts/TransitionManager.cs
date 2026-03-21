using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Estado de Transição")]
    public string targetSpawnPointID;

    [Header("Loading Screen (Opcional)")]
    public GameObject loadingScreenObject; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (loadingScreenObject) loadingScreenObject.SetActive(false);
    }

    // ADIÇÃO CRÍTICA: Parâmetro booleano 'isGameplayScene' (Por padrão é true para não quebrar seus scripts antigos)
    public void TransitionToScene(string sceneName, string spawnPointID, bool isGameplayScene = true)
    {
        Debug.Log($"Transição iniciada para Cena: {sceneName} no Ponto: {spawnPointID}. É Gameplay? {isGameplayScene}");
        
        targetSpawnPointID = spawnPointID; 
        
        StartCoroutine(LoadSceneRoutine(sceneName, isGameplayScene));
    }
    
    // --- SOBRECARGAS PARA BOTÕES DA UNITY (UNITY EVENTS) ---
    // Como botões no Inspector só aceitam 1 parâmetro, criei duas opções fáceis:

    // Arraste isso para o botão de "Novo Jogo" ou "Continuar"
    public void LoadGameplaySceneFromButton(string sceneName)
    {
        TransitionToScene(sceneName, "", true);
    }

    // Arraste isso para botões que levam para telas de "Créditos" ou "Lojas Isoladas"
    public void LoadUISceneFromButton(string sceneName)
    {
        TransitionToScene(sceneName, "", false);
    }
    // --------------------------------------------------------

    private IEnumerator LoadSceneRoutine(string sceneName, bool isGameplayScene)
    {
        if (loadingScreenObject) loadingScreenObject.SetActive(true);

        Time.timeScale = 1f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        
        while (!op.isDone)
        {
            yield return null;
        }

        yield return null; // Espera 1 frame de segurança para a engine respirar
        
        // --- A MÁGICA DO CURSOR E DO INPUT (O Fim dos Bugs) ---
        if (isGameplayScene)
        {
            // Se for fase de jogo: Liga o controle do player, trava o mouse e deixa invisível
            if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("TransitionManager: Ambiente de Gameplay. Cursor travado.");
        }
        else
        {
            // Se for cena de UI (Créditos, Menu, etc): Desliga o player, solta o mouse e deixa visível
            if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("TransitionManager: Ambiente de UI. Cursor livre.");
        }
        // -------------------------------------------------------
        
        if (loadingScreenObject) loadingScreenObject.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("TransitionManager: Iniciando limpeza e retorno ao Menu...");

        if (PlayerPersistent.Instance != null)
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        Time.timeScale = 1f;
        targetSpawnPointID = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }

        if (PauseMenuManager.Instance != null) PauseMenuManager.Instance.ForceHide();
        if (DeathScreenManager.Instance != null) DeathScreenManager.Instance.Hide();

        // CORREÇÃO APLICADA: Carregamento Assíncrono para não travar a tela
        SceneManager.LoadSceneAsync("MainMenu");
    }
}