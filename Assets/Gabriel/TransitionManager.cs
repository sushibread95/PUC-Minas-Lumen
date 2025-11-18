using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    // Esta é a "memória" que sobrevive à troca de cena.
    // Ex: "porta_igreja_entrada"
    public string targetSpawnPointID;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // A porta (InteractableDoor) vai chamar esta função
    public void TransitionToScene(string sceneName, string spawnPointID)
    {
        Debug.Log($"Transição iniciada para Cena: {sceneName} no Ponto: {spawnPointID}");
        
        // 1. Armazena para onde o player deve ir na próxima cena
        targetSpawnPointID = spawnPointID;

        // 2. Carrega a nova cena
        // (No futuro, podemos adicionar um Fade aqui)
        SceneManager.LoadScene(sceneName);
    }
}