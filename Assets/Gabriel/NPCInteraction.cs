// Nome do arquivo: NPCInteraction.cs
// COLOQUE ESTE SCRIPT NO 'Enemy1' (no lugar do FinishableNPC)

using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteraction : MonoBehaviour
{
    [Header("Feedback Visual")]
    [Tooltip("O material de 'brilho' a ser aplicado")]
    public Material highlightMaterial;
    [Tooltip("A mesh do NPC que deve brilhar. Ex: Beta_Surface")]
    public Renderer meshToHighlight;

    // Referências internas (Auto-configuradas)
    private CorruptedNPC npcData;
    private ContextualPromptUI promptUI;
    private Material originalMaterial;
    
    private bool isPlayerClose = false;

    void Awake()
    {
        // 1. Pega os componentes "irmãos"
        npcData = GetComponent<CorruptedNPC>();
        promptUI = GetComponentInChildren<ContextualPromptUI>(true);

        // 2. Salva o material original
        if (meshToHighlight != null)
        {
            originalMaterial = meshToHighlight.material;
        }

        // 3. Garante que a UI de prompt esteja escondida
        promptUI?.Hide();
    }

    // --- LÓGICA DE PROXIMIDADE (HIGHLIGHT) ---

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = true;
            
            // Só aplica o highlight se o NPC ainda estiver "normal"
            if (npcData.currentState == NPCState.Corrompido && meshToHighlight != null && highlightMaterial != null)
            {
                meshToHighlight.material = highlightMaterial;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = false;
            
            // Esconde a UI E o highlight
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
        if (!isPlayerClose) return;

        // ESTADO 1: CORROMPIDO (Player pode Nocautear)
        if (npcData.currentState == NPCState.Corrompido)
        {
            // Checa o input de 'N' (Nocaute)
            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                // 1. Nocauteia o NPC
                npcData.EntrarEmNocaute();
                
                // 2. Mostra as opções (Matar/Purificar)
                promptUI?.Show();
                
                // 3. Tira o highlight (opcional, mas limpo)
                if (meshToHighlight != null && originalMaterial != null)
                {
                    meshToHighlight.material = originalMaterial;
                }
            }
        }
        
        // ESTADO 2: NOCAUTEADO (Player pode Finalizar)
        else if (npcData.currentState == NPCState.Nocauteado)
        {
            // Checa o input de 'P' (Purificar)
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                npcData.SerPurificado();
                promptUI?.Hide();
            }
            
            // Checa o input de 'K' (Matar)
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                npcData.SerMorto();
                promptUI?.Hide();
            }
        }
    }
}