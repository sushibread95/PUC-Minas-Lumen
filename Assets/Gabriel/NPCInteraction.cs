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

    // CONSOLIDAÇÃO (#4): a leitura de Purify/Kill foi REMOVIDA daqui. Agora o
    // CorruptedNPC é o ÚNICO responsável por ler o input e chamar
    // SerPurificado()/SerMorto(), evitando dois scripts processando o mesmo frame.
    // Este componente cuida apenas do highlight e do prompt de proximidade.
}