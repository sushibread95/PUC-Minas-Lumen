using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    // A memória de onde o player quer ir
    public string targetSpawnPointID;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void TransitionToScene(string sceneName, string spawnPointID)
    {
        targetSpawnPointID = spawnPointID; // Guarda o ID na memória
        
        // Opcional: Aqui você pode chamar uma UI de Loading Screen
        SceneManager.LoadScene(sceneName);
    }
    
    // Função auxiliar para o botão "Novo Jogo" chamar
    public void LoadScene(string sceneName, string spawnPointID)
    {
        TransitionToScene(sceneName, spawnPointID);
    }
}