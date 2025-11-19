using UnityEngine;
public class InteractableDoor : MonoBehaviour, IInteractable
{
    public enum DoorType { KeyDoor, SceneTransition }

    [Header("Configuração da Porta")]
    public DoorType doorType;
    public string doorID;

    [Header("Configurações (Key Door)")]
    public Objects requiredKey;
    public bool consumeKeyOnUse = false; 
    public string lockedMessage = "Preciso de uma chave";

    [Header("Configurações (Scene Transition)")]
    public string sceneName;
    public string targetSpawnPointID;

    [Header("Feedback")]
    public Animator doorAnimator;
    public string openAnimationTrigger = "Open";
    
    [Tooltip("Som que toca quando a porta está trancada.")]
    public AudioClip lockedSound;
    [Tooltip("Som que toca quando a porta é destrancada.")]
    public AudioClip unlockSound;
    
    // Removida a variável 'private AudioSource audioSource;'

    private bool isPermanentlyUnlocked = false;

    // --- MODIFICAÇÃO ---
    void Awake()
    {
        // Removida a lógica de pegar o AudioSource local.
    }
    // --- FIM DA MODIFICAÇÃO ---

    void Start()
    {
        // ... (lógica de checagem de estado e load permanece a mesma) ...
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            if (WorldStateManager.Instance.IsDoorUnlocked(doorID))
            {
                isPermanentlyUnlocked = true;
            }
        }
    }

    // --- GetInteractText() (Sem Modificações) ---
    public string GetInteractText()
    {
        if (doorType == DoorType.SceneTransition) return "Entrar";
        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked) return "Abrir";
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
                return $"Usar {requiredKey.objectName}";
            else return lockedMessage;
        }
        return "Interagir";
    }

    // --- Interact() (MODIFICADO) ---
    public void Interact()
    {
        if (doorType == DoorType.SceneTransition)
        {
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.TransitionToScene(sceneName, targetSpawnPointID);
            return;
        }

        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                return;
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
            {
                // --- SUCESSO! ---
                isPermanentlyUnlocked = true;

                if (WorldStateManager.Instance != null)
                    WorldStateManager.Instance.RegisterUnlockedDoor(doorID);
                
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                
                if (consumeKeyOnUse)
                    InventoryManager.Instance.RemoveItem(requiredKey);

                // --- MODIFICAÇÃO DE ÁUDIO ---
                // Toca o som GLOBALMENTE
                if (AudioManager.Instance != null && unlockSound != null)
                    AudioManager.Instance.PlaySFX(unlockSound, transform.position);
            }
            else
            {
                // --- FALHA! ---
                
                // --- MODIFICAÇÃO DE ÁUDIO ---
                // Toca o som GLOBALMENTE
                if (AudioManager.Instance != null && lockedSound != null)
                    AudioManager.Instance.PlaySFX(lockedSound, transform.position);
                
                // Mostra a mensagem na UI
                if (UIFeedbackManager.Instance != null)
                    UIFeedbackManager.Instance.ShowNotification(lockedMessage, 2f);
            }
        }
    }
}