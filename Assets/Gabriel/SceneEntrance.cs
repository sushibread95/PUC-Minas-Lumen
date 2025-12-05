using UnityEngine;

public class SceneEntrance : MonoBehaviour
{
    [Header("Identidade deste Ponto")]
    [Tooltip("Ex: 'fase1_spawn', 'entrada_caverna'. Deve bater com o ID que estava na Porta da cena anterior.")]
    public string mySpawnID;

    [Header("Para Novo Jogo")]
    [Tooltip("Arraste o Prefab do Player aqui apenas para o Spawn Inicial do jogo.")]
    public GameObject playerPrefab;

    void Start()
    {
        // 1. Verifica se devo ativar este spawn
        if (TransitionManager.Instance != null)
        {
            // Se o TransitionManager tem um ID na memória, verifica se sou eu
            string targetID = TransitionManager.Instance.targetSpawnPointID;

            if (!string.IsNullOrEmpty(targetID))
            {
                if (targetID != mySpawnID) return; // Não é pra mim, tchau.
            }
            else
            {
                // Se não tem ID (Start Game), só roda se eu for o padrão
                if (mySpawnID != "fase1_spawn") return;
            }
        }

        // 2. Lógica de Spawn ou Teleporte
        if (PlayerPersistent.Instance != null)
        {
            // CASO A: Player já existe (viajando entre cenas) -> TELEPORTA
            Debug.Log($"[SceneEntrance] Player Persistente detectado em '{mySpawnID}'. Teleportando.");
            
            // Desliga CharacterController para mover sem bugar
            var cc = PlayerPersistent.Instance.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            PlayerPersistent.Instance.transform.position = transform.position;
            PlayerPersistent.Instance.transform.rotation = transform.rotation;

            if (cc) cc.enabled = true;
        }
        else if (playerPrefab != null)
        {
            // CASO B: Nenhum Player existe (Novo Jogo) -> CRIA
            Debug.Log($"[SceneEntrance] Criando Player Inicial em '{mySpawnID}'.");
            Instantiate(playerPrefab, transform.position, transform.rotation);
        }

        // Limpa a memória do Manager para não spawnar errado depois
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.targetSpawnPointID = null;
        }
    }
}