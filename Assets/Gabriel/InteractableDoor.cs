using System.Collections;
using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    public enum DoorType { KeyDoor, SceneTransition }

    [Header("--- TIPO DE PORTA ---")]
    public DoorType doorType;
    public string doorID; // ID para o sistema de chaves (Save)

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
        // 1. Lógica de Chave (Carregar Save)
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            if (WorldStateManager.Instance.IsDoorUnlocked(doorID)) isPermanentlyUnlocked = true;
        }

        // 2. Lógica de Chegada (Spawn/Teleporte)
        HandleArrival();
    }

    // --- LÓGICA DE CHEGADA (Vinda do antigo SceneEntrance) ---
private void HandleArrival()
{
    // Se eu não tenho ID de chegada, sou apenas uma porta de saída. Ignora.
    if (string.IsNullOrEmpty(mySpawnID)) return;

    // Se o TransitionManager não existir ou o ID não bater, tchau.
    if (TransitionManager.Instance != null)
    {
        string target = TransitionManager.Instance.targetSpawnPointID;
        
        if (string.IsNullOrEmpty(target))
        {
            if (mySpawnID != "fase1_spawn") return;

            // Se não tem ID (target vazio), é "Novo Jogo".
            // Mas se o Player JÁ EXISTE, não é Novo Jogo
            if (PlayerPersistent.Instance != null) return;
        }
        else if (target != mySpawnID)
        {
            return; 
        }
    }
    else
    {
        // Fallback sem manager
        if (mySpawnID != "fase1_spawn") return;
    }

        // --- DEFINIR POSIÇÃO ---
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        // --- SPAWN OU TELEPORTE ---
        if (PlayerPersistent.Instance != null)
        {
            Debug.Log($"[Door] Player chegou em '{mySpawnID}'. Teleportando.");
            var cc = PlayerPersistent.Instance.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            PlayerPersistent.Instance.transform.SetPositionAndRotation(pos, rot);
            if (cc) cc.enabled = true;
        }
        else if (playerPrefab != null)
        {
            Debug.Log($"[Door] Criando Player Inicial em '{mySpawnID}'.");
            Instantiate(playerPrefab, pos, rot);
        }

                StartCoroutine(ForceGameplayInputAfterSpawn());

            // Limpa o Manager
            if (TransitionManager.Instance != null) 
                TransitionManager.Instance.targetSpawnPointID = null;
        }     

     
        private IEnumerator ForceGameplayInputAfterSpawn()
        {
            // Espera 2 frames para garantir que o Player foi completamente inicializado
            yield return null;
            yield return null;
            
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SwitchToGameplayMap();
                Debug.Log("🎮 InteractableDoor: Forçou Input para Gameplay após spawn!");
            }
            
            // Garante que o cursor fique travado
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnID);
    }
}