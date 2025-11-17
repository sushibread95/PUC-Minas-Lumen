using UnityEngine;
using UnityEngine.InputSystem; // Importa o novo sistema

// CÓDIGO ATUALIZADO
public class NPCInteraction : MonoBehaviour
{
    [Header("Feedback Visual")]
    [Tooltip("O material de 'brilho' a ser aplicado")]
    public Material highlightMaterial;
    [Tooltip("A mesh do NPC que deve brilhar. Ex: Beta_Surface")]
    public Renderer meshToHighlight;

    private CorruptedNPC npcData;
    private ContextualPromptUI promptUI;
    private Material originalMaterial;

    private bool isPlayerClose = false;

    // --- INÍCIO DAS MUDANÇAS ---
    private PlayerInputActions input;
    private bool inputInitialized = false;
    // --- FIM DAS MUDANÇAS ---

    void Awake()
    {
        npcData = GetComponent<CorruptedNPC>();
        promptUI = GetComponentInChildren<ContextualPromptUI>(true);

        if (meshToHighlight != null)
        {
            originalMaterial = meshToHighlight.material;
        }

        promptUI?.Hide();
    }

    // --- LÓGICA DE PROXIMIDADE (HIGHLIGHT) ---

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // --- INÍCIO DAS MUDANÇAS ---
            // Pega o InputManager na primeira vez que o player se aproxima
            if (!inputInitialized && InputManager.Instance != null)
            {
                input = InputManager.Instance.InputActions;
                inputInitialized = true;
            }
            // --- FIM DAS MUDANÇAS ---

            isPlayerClose = true;

            if (npcData.currentState == NPCState.Corrompido && meshToHighlight != null && highlightMaterial != null)
            {
                meshToHighlight.material = highlightMaterial;
            }
            else if (npcData.currentState == NPCState.Nocauteado)
            {
                promptUI?.Show();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = false;

            promptUI?.Hide();
            if (meshToHighlight != null && originalMaterial != null)
            {
                meshToHighlight.material = originalMaterial;
            }
        }
    }

    // --- LÓGICA DE SIMULAÇÃO (INPUTS) ---

    void Update()
    {
        // Cláusula de guarda: não faz nada se o player não estiver perto
        // OU se o input não foi pego (porque o InputManager não existe)
        if (!isPlayerClose || !inputInitialized) return;

        // ESTADO 1: CORROMPIDO (Player pode Nocautear)
        if (npcData.currentState == NPCState.Corrompido)
        {
            // (Input de Nocaute 'N' removido para focar no 'Fallen')
            // (O dano normal agora vai derrubar o inimigo)
        }

        // ESTADO 2: NOCAUTEADO (Player pode Finalizar)
        else if (npcData.currentState == NPCState.Nocauteado)
        {
            // --- INÍCIO DAS MUDANÇAS ---
            // Checa o input de 'Purify' (P) do PlayerInputActions
            if (input.Player.Purify.WasPressedThisFrame())
            {
                npcData.SerPurificado();
                promptUI?.Hide();
            }

            // Checa o input de 'Kill' (K) do PlayerInputActions
            if (input.Player.Kill.WasPressedThisFrame())
            {
                npcData.SerMorto();
                promptUI?.Hide();
            }
            // --- FIM DAS MUDANÇAS ---
        }
    }
}