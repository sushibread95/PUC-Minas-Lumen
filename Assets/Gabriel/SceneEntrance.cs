using UnityEngine;

public class SceneEntrance : MonoBehaviour
{
    [Header("Configuração de Spawn")]
    public string mySpawnID;

    [Header("Prefab do Player")]
    public GameObject playerPrefab;

    void Awake()
    {
        bool isDefaultStart = false;

        // 1. Lógica de Checagem
        if (TransitionManager.Instance != null)
        {
            string targetID = TransitionManager.Instance.targetSpawnPointID;

            if (string.IsNullOrEmpty(targetID))
            {
                // Cenário de 'StartNewGame' (sem ID setada)
                // Se o ID for o ID de spawn inicial, a gente prossegue.
                if (mySpawnID == "fase1_spawn")
                {
                    isDefaultStart = true;
                }
                else
                {
                    return; // É um spawn point que não é o inicial, então não deve rodar
                }
            }
            else if (targetID != mySpawnID)
            {
                return; // ID setada, mas não é a ID correta para esta porta
            }
        }
        else
        {
             // Fallback: Se não tem TransitionManager, só deve rodar o spawn inicial
             if (mySpawnID != "fase1_spawn") return;
             isDefaultStart = true;
        }

        // --- SE CHEGOU AQUI, DEVE SPAWNAR OU TELEPORTAR ---
        
        // 1. Verifica se um player já existe
        PlayerControllerSystem existingPlayer = FindFirstObjectByType<PlayerControllerSystem>();
        if (existingPlayer != null)
        {
            Debug.LogWarning($"SceneEntrance: Um Player já existe na cena. Teleportando-o.");
            existingPlayer.transform.position = transform.position;
            return;
        }
        
        // 2. Cria (instancia) o Player
        if (playerPrefab != null)
        {
            Debug.Log($"SceneEntrance [{mySpawnID}]: Instanciando Player na posição {transform.position}");
            Instantiate(playerPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError($"SceneEntrance [{mySpawnID}]: Prefab do Player não configurado!");
        }

        // 3. Limpa a ID (para não spawnar em outro lugar por acidente)
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.targetSpawnPointID = null;
        }
    }
}