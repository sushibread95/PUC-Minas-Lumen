using UnityEngine;

public class PlayerPersistent : MonoBehaviour
{
    #region Singleton

    public static PlayerPersistent Instance { get; private set; }

    #endregion

    #region Inspector

    [Header("--- PERSISTÊNCIA ---")]
    [SerializeField] private bool keepPlayerBetweenScenes = true;
    [SerializeField] private bool destroyDuplicatePlayers = true;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (destroyDuplicatePlayers)
            {
                Debug.LogWarning($"PlayerPersistent: Player duplicado removido: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        if (keepPlayerBetweenScenes)
        {
            // DontDestroyOnLoad só funciona corretamente em objetos raiz.
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }

        RegisterOnSaveManager();
    }

    private void Start()
    {
        RegisterOnSaveManager();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Registration

    // Mantém o SaveManager sempre apontando para o player válido.
    private void RegisterOnSaveManager()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(transform);
    }

    #endregion
}
