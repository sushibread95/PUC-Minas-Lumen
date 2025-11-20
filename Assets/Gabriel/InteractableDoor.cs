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
    [Tooltip("Nome da cena para onde vai. Se for uma KeyDoor, preencha isso para viajar APÓS destrancar.")]
    public string sceneName;
    public string targetSpawnPointID;

    [Header("Feedback")]
    public Animator doorAnimator;
    public string openAnimationTrigger = "Open";
    
    [Tooltip("Som que toca quando a porta está trancada.")]
    public AudioClip lockedSound;
    [Tooltip("Som que toca quando a porta é destrancada.")]
    public AudioClip unlockSound;

    private bool isPermanentlyUnlocked = false;

    void Awake() { }

    void Start()
    {
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            if (WorldStateManager.Instance.IsDoorUnlocked(doorID))
            {
                isPermanentlyUnlocked = true;
            }
        }
    }

    public string GetInteractText()
    {
        if (doorType == DoorType.SceneTransition) return "Entrar";
        
        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                // --- MELHORIA VISUAL ---
                // Se está destrancada e leva a algum lugar, diz "Entrar"
                if (!string.IsNullOrEmpty(sceneName)) return "Entrar";
                return "Abrir";
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
                return $"Usar {requiredKey.objectName}";
            else 
                return lockedMessage;
        }
        return "Interagir";
    }

    public void Interact()
    {
        // Tipo 1: Transição Simples (Sempre aberta)
        if (doorType == DoorType.SceneTransition)
        {
            PerformTransition();
            return;
        }

        // Tipo 2: Porta com Chave
        if (doorType == DoorType.KeyDoor)
        {
            // Se JÁ ESTÁ DESTRANCADA...
            if (isPermanentlyUnlocked)
            {
                // --- CORREÇÃO PRINCIPAL ---
                // Verifica se essa porta leva para algum lugar
                if (!string.IsNullOrEmpty(sceneName))
                {
                    PerformTransition();
                }
                else
                {
                    // Se não leva a lugar nenhum (ex: um armário), só anima
                    if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                }
                return;
            }

            // Se ESTÁ TRANCADA, tenta abrir...
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
            {
                // SUCESSO: Destranca
                isPermanentlyUnlocked = true;

                if (WorldStateManager.Instance != null)
                    WorldStateManager.Instance.RegisterUnlockedDoor(doorID);
                
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                
                if (consumeKeyOnUse)
                    InventoryManager.Instance.RemoveItem(requiredKey);

                if (AudioManager.Instance != null && unlockSound != null)
                    AudioManager.Instance.PlaySFX(unlockSound, transform.position);
                
                // Nota: Na primeira interação ele apenas destranca. 
                // O jogador deve clicar de novo (agora que o texto virou "Entrar") para viajar.
            }
            else
            {
                // FALHA: Sem chave
                if (AudioManager.Instance != null && lockedSound != null)
                    AudioManager.Instance.PlaySFX(lockedSound, transform.position);
                
                if (UIFeedbackManager.Instance != null)
                    UIFeedbackManager.Instance.ShowNotification(lockedMessage, 2f);
            }
        }
    }

    private void PerformTransition()
    {
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene(sceneName, targetSpawnPointID);
    }
}