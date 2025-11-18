using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Precisamos disso para a UI de "dica"

public class InteractionManager : MonoBehaviour
{
    // --- AVISO DE MODIFICAÇÃO ---
    // A lógica de "Inventory Settings" (slots, inventoryUIParent) foi REMOVIDA
    // pois seu novo script InventoryController.cs já cuida disso.
    // --- FIM DO AVISO ---

    private PlayerInputActions input;
    private IInteractable currentTarget; // O que estamos olhando (agora é genérico)

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    // --- AVISO DE MODIFICAÇÃO ---
    // 'itemTag' e 'itemLayer' foram substituídos por 'interactionMask'
    // para que o Raycast possa acertar Portas, Placas, etc.
    public LayerMask interactionMask;
    // --- FIM DO AVISO ---
    public Material highlightMaterial; // Mantido do seu script
    private Material originalMaterial;  // Mantido do seu script
    private Renderer currentTargetRenderer; // Para guardar o Renderer

    // --- ADIÇÃO ---
    [Header("UI de Feedback")]
    [Tooltip("O 'prompt' de texto (ex: [E] Pegar Chave)")]
    public TextMeshProUGUI interactionText; // Arraste sua UI de texto aqui
    public Camera playerCamera; // Referência da câmera para o Raycast
    // --- FIM DA ADIÇÃO ---

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (interactionText != null) interactionText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // A inscrição no InventoryManager foi removida
        // pois este script não mexe mais na UI do inventário.
    }

    void OnDisable()
    {
        if (input != null && InputManager.Instance != null)
        {
            input.Player.Interact.performed -= OnInteractPressed;
        }
        // A desinscrição do InventoryManager foi removida.
    }

    void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InteractionManager não encontrou o InputManager!");
            this.enabled = false;
            return;
        }
        input = InputManager.Instance.InputActions;
        input.Player.Interact.performed += OnInteractPressed;

        // --- AVISO DE MODIFICAÇÃO ---
        // A lógica de 'Instantiate(slotPrefab)' foi REMOVIDA
        // pois seu novo InventoryController.cs já cuida disso.
        // --- FIM DO AVISO ---
    }

    void Update()
    {
        // "Guarda Mestra" para todos os menus
        if (input == null ||
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
        {
            ClearHighlight(); // Limpa o highlight se o jogo pausar
            return;
        }

        DetectInteractable(); // Renomeado de DetectItemInFront
    }

    // --- FUNÇÃO MODIFICADA ---
    private void DetectInteractable()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        IInteractable newTarget = null;
        Renderer newRenderer = null;

        // O Raycast agora usa a MÁSCARA (LayerMask), não a Layer antiga
        if (Physics.Raycast(ray, out hit, pickupRange, interactionMask))
        {
            // Em vez de checar a TAG, procuramos o "contrato" IInteractable
            newTarget = hit.collider.GetComponentInParent<IInteractable>();
            
            if (newTarget != null)
            {
                // Pega o renderer para o highlight
                newRenderer = hit.collider.GetComponentInChildren<Renderer>();
            }
        }

        // --- Lógica de Mudança de Alvo ---
        if (newTarget != currentTarget)
        {
            // Limpa o alvo antigo (highlight e texto)
            ClearHighlight();

            if (newTarget != null)
            {
                // Configura o novo alvo
                currentTarget = newTarget;
                currentTargetRenderer = newRenderer;

                // Mostra o texto de "dica" (ex: "[E] Abrir Porta")
                if (interactionText != null)
                {
                    interactionText.text = $"[E] {currentTarget.GetInteractText()}";
                    interactionText.gameObject.SetActive(true);
                }
                HighlightItem(); // Aplica o highlight
            }
        }
    }

    // --- FUNÇÃO MODIFICADA ---
    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        // Se estamos olhando para algo interativo E o jogo não está pausado...
        if (currentTarget != null &&
           (PauseMenuManager.Instance == null || !PauseMenuManager.Instance.IsPaused) &&
           (InventoryController.Instance == null || !InventoryController.Instance.IsInventoryOpen))
        {
            // --- AVISO DE MODIFICAÇÃO (LÓGICA MOVIDA) ---
            // Toda a lógica de AddItem, RegisterCollectedItem e Destroy
            // foi MOVIDA para o script ObjectType.cs.
            // Este script agora apenas "chama" a interação.
            // --- FIM DO AVISO ---
            currentTarget.Interact();
            
            ClearHighlight();
        }
    }

    // --- Funções de Highlight (Modificadas levemente) ---
    private void HighlightItem()
    {
        if (currentTargetRenderer != null && highlightMaterial != null)
        {
            originalMaterial = currentTargetRenderer.material;
            currentTargetRenderer.material = highlightMaterial;
        }
    }

    private void ClearHighlight()
    {
        if (currentTargetRenderer != null && originalMaterial != null)
        {
            currentTargetRenderer.material = originalMaterial;
        }
        
        // Limpa o texto da UI
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }

        currentTarget = null;
        currentTargetRenderer = null;
        originalMaterial = null;
    }

    // --- AVISO DE MODIFICAÇÃO ---
    // A função 'UpdateUIFromManager()' foi REMOVIDA
    // pois este script não controla mais a UI do inventário.
    // --- FIM DO AVISO ---
}