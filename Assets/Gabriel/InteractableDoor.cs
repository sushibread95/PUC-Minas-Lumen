using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Garante que a porta possa tocar som
public class InteractableDoor : MonoBehaviour, IInteractable
{
    public enum DoorType { KeyDoor, SceneTransition }

    [Header("Configuração da Porta")]
    public DoorType doorType;
    [Tooltip("ID única desta porta para o SaveManager. Ex: 'porta_igreja_vilarejo'")]
    public string doorID;

    [Header("Configurações (Key Door)")]
    [Tooltip("Se for 'KeyDoor', arraste o ScriptableObject da chave necessária aqui.")]
    public Objects requiredKey;
    public bool consumeKeyOnUse = false; 
    public string lockedMessage = "Preciso de uma chave";

    [Header("Configurações (Scene Transition)")]
    [Tooltip("O nome exato da cena para onde esta porta leva.")]
    public string sceneName;
    [Tooltip("A ID do 'SceneEntrance' na próxima cena. Ex: 'igreja_entrada_principal'")]
    public string targetSpawnPointID;

    [Header("Feedback")]
    [Tooltip("(Opcional) O Animator da porta para tocar a animação 'Open'")]
    public Animator doorAnimator;
    public string openAnimationTrigger = "Open";
    
    [Tooltip("Som que toca quando a porta está trancada.")]
    public AudioClip lockedSound;
    [Tooltip("Som que toca quando a porta é destrancada.")]
    public AudioClip unlockSound;
    private AudioSource audioSource;

    // Variável de estado
    private bool isPermanentlyUnlocked = false;

    void Awake()
    {
        // Pega o AudioSource que adicionamos com [RequireComponent]
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // Ao iniciar, checa no WorldStateManager se esta porta já foi destrancada
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            if (WorldStateManager.Instance.IsDoorUnlocked(doorID))
            {
                isPermanentlyUnlocked = true;
            }
        }
    } // <- O colchete '}' que provavelmente estava faltando

    // --- Implementação do Contrato IInteractable ---

    public string GetInteractText()
    {
        if (doorType == DoorType.SceneTransition)
        {
            return "Entrar";
        }

        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                return "Abrir"; // A porta já foi destrancada
            }
            
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
            {
                return $"Usar {requiredKey.objectName}";
            }
            else
            {
                return lockedMessage; // Ex: "Preciso de uma chave"
            }
        }
        return "Interagir";
    }

    public void Interact()
    {
        // --- LÓGICA DE TRANSIÇÃO DE CENA ---
        if (doorType == DoorType.SceneTransition)
        {
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.TransitionToScene(sceneName, targetSpawnPointID);
            }
            else
            {
                Debug.LogError($"InteractableDoor: TransitionManager.Instance é nulo!");
            }
            return;
            
        }

        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                // --- ADIÇÃO: SE ESTIVER DESTRANCADA E TIVER CENA, ENTRA ---
                if (!string.IsNullOrEmpty(sceneName))
                {
                    if (TransitionManager.Instance != null)
                        TransitionManager.Instance.TransitionToScene(sceneName, targetSpawnPointID);
                    return;
                }
                // ----------------------------------------------------------

                // Porta já está destrancada (e não leva a lugar nenhum), apenas toca a animação
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                return;
            }
        }

        // --- LÓGICA DE PORTA COM CHAVE ---
        if (doorType == DoorType.KeyDoor)
        {
            if (isPermanentlyUnlocked)
            {
                // Porta já está destrancada, apenas toca a animação
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);
                return;
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey))
            {
                // --- SUCESSO! O PLAYER TEM A CHAVE ---
                Debug.Log($"Porta {doorID} destrancada com {requiredKey.objectName}!");
                isPermanentlyUnlocked = true;

                // 1. Salva o estado "destrancada"
                if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(doorID))
                {
                    WorldStateManager.Instance.RegisterUnlockedDoor(doorID);
                }

                // 2. Toca a animação
                if (doorAnimator != null) doorAnimator.SetTrigger(openAnimationTrigger);

                // 3. (Opcional) Remove a chave do inventário
                if (consumeKeyOnUse)
                {
                    InventoryManager.Instance.RemoveItem(requiredKey);
                }
                
                // 4. Toca o som de "destrancar"
                if (audioSource != null && unlockSound != null)
                    audioSource.PlayOneShot(unlockSound);
            }
            else
            {
                // --- FALHA! O PLAYER NÃO TEM A CHAVE ---
                Debug.Log($"Porta {doorID} está trancada. Falta a chave: {requiredKey.objectName}");

                // 1. Toca o som de "trancado"
                if (audioSource != null && lockedSound != null)
                    audioSource.PlayOneShot(lockedSound);
                
                // 2. Mostra a mensagem na tela
                if (UIFeedbackManager.Instance != null)
                    UIFeedbackManager.Instance.ShowNotification(lockedMessage, 2f);
            }
        }
    }
} 