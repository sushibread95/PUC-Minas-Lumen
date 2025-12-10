using UnityEngine;
using UnityEngine.SceneManagement; // ADICIONADO PARA CARREGAR CENA DIRETO

public class FinalDoor : MonoBehaviour, IInteractable
{
    public enum DoorType { KeyDoor, SceneTransition }

    [Header("--- TIPO DE PORTA ---")]
    public DoorType doorType;
    public string doorID; 

    [Header("--- FIM DE JOGO (NOVO) ---")]
    [Tooltip("Se marcado, ao entrar, o jogo destroi o Player e carrega a cena final sem transição de spawn.")]
    public bool isVictoryDoor = false; // <--- A MÁGICA ESTÁ AQUI

    [Header("--- SAÍDA (Para onde vou?) ---")]
    [Tooltip("Nome da cena para carregar.")]
    public string targetSceneName;
    [Tooltip("ID do ponto onde o player deve nascer na próxima cena.")]
    public string targetSpawnID;

    [Header("--- CHEGADA (Quem chega aqui?) ---")]
    [Tooltip("ID deste ponto nesta cena. Se vazio, esta porta não serve como spawn.")]
    public string mySpawnID; 
    [Tooltip("Arraste um objeto vazio filho aqui. É onde o player vai aparecer.")]
    public Transform spawnPoint; 
    [Tooltip("Prefab do Player (Só necessário se for o Spawn Inicial do jogo)")]
    public GameObject playerPrefab;

    [Header("--- CONFIGURAÇÃO DE CHAVE ---")]
    public Objects requiredKey;
    public bool consumeKeyOnUse = false; 
    public string lockedMessage = "Preciso de uma chave";

    [Header("--- FEEDBACK ---")]
    public Animator doorAnimator;
    public string openAnimationTrigger = "Open";
    public AudioClip lockedSound;
    public AudioClip unlockSound;

    private bool isPermanentlyUnlocked = false;

    void Start()
    {
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            if (WorldStateManager.Instance.IsDoorUnlocked(doorID)) isPermanentlyUnlocked = true;
        }
        HandleArrival();
    }

    private void HandleArrival()
    {
        if (string.IsNullOrEmpty(mySpawnID)) return;

        if (TransitionManager.Instance != null)
        {
            string target = TransitionManager.Instance.targetSpawnPointID;
            
            if (string.IsNullOrEmpty(target))
            {
                if (mySpawnID != "fase1_spawn") return;
                if (PlayerPersistent.Instance != null) return;
            }
            else if (target != mySpawnID)
            {
                return; 
            }
        }
        else
        {
            if (mySpawnID != "fase1_spawn") return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        if (PlayerPersistent.Instance != null)
        {
            var cc = PlayerPersistent.Instance.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            PlayerPersistent.Instance.transform.SetPositionAndRotation(pos, rot);
            if (cc) cc.enabled = true;
        }
        else if (playerPrefab != null)
        {
            Instantiate(playerPrefab, pos, rot);
        }

        if (TransitionManager.Instance != null) TransitionManager.Instance.targetSpawnPointID = null;
    }

    public string GetInteractText()
    {
        if (doorType == DoorType.SceneTransition) return "Entrar";
        
        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                if (!string.IsNullOrEmpty(targetSceneName)) return "Entrar";
                return "Abrir";
            }
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
                return $"Usar {requiredKey.objectName}";
            
            return lockedMessage;
        }
        return "Interagir";
    }

    public void Interact()
    {
        if (doorType == DoorType.SceneTransition)
        {
            PerformTransition();
            return;
        }

        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                if (!string.IsNullOrEmpty(targetSceneName)) PerformTransition();
                else if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                return;
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
            {
                isPermanentlyUnlocked = true;
                if (WorldStateManager.Instance != null) WorldStateManager.Instance.RegisterUnlockedDoor(doorID);
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                if (consumeKeyOnUse) InventoryManager.Instance.RemoveItem(requiredKey);
                if (AudioManager.Instance != null && unlockSound != null) AudioManager.Instance.PlaySFX(unlockSound, transform.position);
            }
            else
            {
                if (AudioManager.Instance != null && lockedSound != null) AudioManager.Instance.PlaySFX(lockedSound, transform.position);
                if (UIFeedbackManager.Instance != null) UIFeedbackManager.Instance.ShowNotification(lockedMessage, 2f);
            }
        }
    }

    private void PerformTransition()
    {
        // --- LÓGICA ESPECIAL DE VITÓRIA ---
        if (isVictoryDoor)
        {
            // 1. Destrava o cursor para o jogador poder clicar nos botões da tela final
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 2. Destroi o Player Persistente (para ele não aparecer na tela de vitória)
            if (PlayerPersistent.Instance != null)
            {
                Destroy(PlayerPersistent.Instance.gameObject);
            }

            // 3. (Opcional) Destroi o Canvas de HUD se ele for persistente também
            // if (HUDManager.Instance != null) Destroy(HUDManager.Instance.gameObject);

            // 4. Carrega a cena de Vitória diretamente (sem passar pelo TransitionManager)
            SceneManager.LoadScene(targetSceneName);
            return;
        }
        // ----------------------------------

        // Fluxo Normal de Jogo
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnID);
    }
}