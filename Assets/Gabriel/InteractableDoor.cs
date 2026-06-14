using UnityEngine;
using UnityEngine.AI;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    #region Inspector

    [Header("--- TIPO DE PORTA ---")]
    [SerializeField] private string doorID = "porta_id";
    [SerializeField] private string interactText = "Interagir";

    [Header("--- TELEPORTE NA MESMA CENA ---")]
    [SerializeField] private Transform teleportWaypoint;
    [SerializeField] private bool matchWaypointRotation = true;

    [Header("--- CONFIGURAÇÃO DE CHAVE ---")]
    [SerializeField] private Objects requiredKey;
    [SerializeField] private bool consumeKeyOnUse = true;
    [SerializeField] private string lockedMessage = "Preciso de uma chave";

    [Header("--- FEEDBACK ---")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip unlockSound;

    #endregion

    #region Interaction

    // Executa a interação principal da porta.
    public void Interact()
    {
        if (!CanOpenDoor())
        {
            PlayLockedFeedback();
            return;
        }

        ConsumeRequiredKey();
        PlayOpenFeedback();
        TeleportPlayer();
    }

    // Retorna o texto exibido no prompt de interação.
    public string GetInteractText()
    {
        return interactText;
    }

    #endregion

    #region Door Logic

    // Verifica se a porta pode ser usada.
    private bool CanOpenDoor()
    {
        if (requiredKey == null)
            return true;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(InteractableDoor)}] InventoryManager não encontrado.");
            return false;
        }

        return InventoryManager.Instance.HasItem(requiredKey);
    }

    // Consome a chave caso a porta esteja configurada para isso.
    private void ConsumeRequiredKey()
    {
        if (requiredKey == null || !consumeKeyOnUse)
            return;

        if (InventoryManager.Instance != null) 
            InventoryManager.Instance.RemoveItem(requiredKey);
    }

    #endregion

    #region Teleport

    // Teleporta o jogador para o waypoint configurado na mesma cena.
    private void TeleportPlayer()
    {
        if (teleportWaypoint == null)
        {
            Debug.LogWarning($"[{nameof(InteractableDoor)}] Waypoint não configurado na porta: {gameObject.name}");
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning($"[{nameof(InteractableDoor)}] Nenhum objeto com tag Player encontrado.");
            return;
        }

        Transform player = playerObject.transform;

        CharacterController characterController = player.GetComponent<CharacterController>();
        NavMeshAgent navMeshAgent = player.GetComponent<NavMeshAgent>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (characterController != null)
            characterController.enabled = false;

        if (navMeshAgent != null && navMeshAgent.enabled)
            navMeshAgent.Warp(teleportWaypoint.position);
        else
            player.position = teleportWaypoint.position;

        if (matchWaypointRotation)
            player.rotation = teleportWaypoint.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (characterController != null)
            characterController.enabled = true;
    }

    #endregion

    #region Feedback

    // Toca feedback de porta trancada.
    private void PlayLockedFeedback()
    {
        if (!string.IsNullOrEmpty(lockedMessage))
        {
            Debug.Log(lockedMessage);
            // Mostra a dica NA TELA (antes só ia para o console, invisível ao player).
            if (UIFeedbackManager.Instance != null)
                UIFeedbackManager.Instance.ShowNotification(lockedMessage, 2f);
        }

        if (lockedSound != null)
            AudioSource.PlayClipAtPoint(lockedSound, transform.position);
    }

    // Toca feedback de abertura da porta.
    private void PlayOpenFeedback()
    {
        if (doorAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
            doorAnimator.SetTrigger(openAnimationTrigger);

        if (unlockSound != null)
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);
    }

    #endregion
}