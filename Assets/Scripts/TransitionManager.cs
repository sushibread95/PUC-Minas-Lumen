using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Estado de Transição")]
    // A memória de onde o player quer ir na próxima cena
    public string targetSpawnPointID;

    // Opcional: Se quiser adicionar Loading Screen no futuro
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
        
        // Garante que loading screen comece desligada
        if (loadingScreenObject) loadingScreenObject.SetActive(false);
    }

    // Método principal chamado pelas Portas e pelo Menu "Novo Jogo"
    public void TransitionToScene(string sceneName, string spawnPointID)
    {
        Debug.Log($"Transição iniciada para Cena: {sceneName} no Ponto: {spawnPointID}");
        
        targetSpawnPointID = spawnPointID; // Guarda o ID na memória persistente
        
        StartCoroutine(LoadSceneRoutine(sceneName));
    }
    
    // Sobrecarga para facilitar chamadas via Unity Event (Botões)
    public void LoadScene(string sceneName, string spawnPointID)
    {
        TransitionToScene(sceneName, spawnPointID);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 1. Ativa Loading Screen (se houver)
        if (loadingScreenObject) loadingScreenObject.SetActive(true);

        // 2. Garante que o tempo esteja normal antes de carregar
        Time.timeScale = 1f;

        // 3. Carrega a cena de forma assíncrona
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        
        // Impede a ativação imediata se quiser fazer fade-in/out (opcional)
        // op.allowSceneActivation = false; 

        while (!op.isDone)
        {
            // Aqui você pode atualizar uma barra de progresso: op.progress
            yield return null;
        }

        // 4. Cena carregada!
        // O SceneEntrance da nova cena vai ler o 'targetSpawnPointID' no Start()
        
        if (loadingScreenObject) loadingScreenObject.SetActive(false);
    }

    // Método utilitário para voltar ao Menu com limpeza total

public void ReturnToMainMenu()
{
    Debug.Log("TransitionManager: Iniciando limpeza e retorno ao Menu...");

    // 1. Destrói o Player Persistente (A CORREÇÃO PRINCIPAL)
    // Isso impede que o Update() do Player continue rodando e travando o mouse
    if (PlayerPersistent.Instance != null)
    {
        Destroy(PlayerPersistent.Instance.gameObject);
    }

    // 2. Reseta o Tempo (caso venha de Pause/Morte)
    Time.timeScale = 1f;

    // 3. Limpa ID de Spawn para não teletransportar incorretamente no futuro
    targetSpawnPointID = null;

    // 4. Força o Mouse a aparecer (Segurança extra para o Menu)
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    // 5. Força o Input para UI (Para navegar nos botões)
    if (InputManager.Instance != null)
    {
        InputManager.Instance.SwitchToUIMap();
    }

    // 6. Fecha interfaces residuais
    if (PauseMenuManager.Instance != null) PauseMenuManager.Instance.ForceHide();
    if (DeathScreenManager.Instance != null) DeathScreenManager.Instance.Hide();

    // 7. Carrega a cena
    SceneManager.LoadScene("MainMenu");
}

}