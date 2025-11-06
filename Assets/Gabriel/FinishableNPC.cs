// Nome do arquivo: FinishableNPC.cs
// CÓDIGO COMPLETO E LIMPO (COM LÓGICA DE HIGHLIGHT)

using UnityEngine;
using UnityEngine.InputSystem; 

public class FinishableNPC : MonoBehaviour
{
    [HideInInspector]
    public CorruptedNPC npcData; 
    
    // --- NOVO ---
    [Header("Feedback Visual")]
    [Tooltip("O material de 'brilho' a ser aplicado")]
    public Material highlightMaterial;
    [Tooltip("A mesh do NPC que deve brilhar. Ex: Beta_Surface")]
    public Renderer meshToHighlight;

    private Material originalMaterial; // Para guardar o material original
    private bool isHighlighted = false;
    // --- FIM DO NOVO ---

    private bool isPlayerClose = false;
    private ContextualPromptUI promptUI;
    
    // O script começa "desligado" (enabled = false)
    // A função Start() só roda quando ele é "acordado" (enabled = true)
    void Start()
    {
        // Ao acordar, ele procura sua UI
        promptUI = GetComponentInChildren<ContextualPromptUI>(true);
        if (promptUI == null)
        {
            Debug.LogError("FinishableNPC não conseguiu encontrar a ContextualPromptUI em seus filhos!");
        }

        // --- NOVO ---
        // Salva o material original UMA VEZ, assim que é nocauteado
        if (meshToHighlight != null)
        {
            originalMaterial = meshToHighlight.material;
        }
        // --- FIM DO NOVO ---
    }

    // O CorruptedNPC chama esta função para passar os dados
    public void WakeUp(CorruptedNPC npc)
    {
        this.npcData = npc;
    }
    
    void OnDisable()
    {
        // Se o script for desabilitado (pelo NPC morrendo ou manualmente),
        // garante que a UI suma e o material original seja restaurado.
        if (isPlayerClose && promptUI != null)
        {
            promptUI.Hide();
            isPlayerClose = false;
        }

        // --- NOVO ---
        // Garante que o material original seja restaurado
        if (isHighlighted && meshToHighlight != null && originalMaterial != null)
        {
            meshToHighlight.material = originalMaterial;
            isHighlighted = false;
        }
        // --- FIM DO NOVO ---
    }

    // Lógica do Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = true;
            promptUI?.Show();

            // --- NOVO ---
            // Aplica o highlight
            if (meshToHighlight != null && highlightMaterial != null)
            {
                meshToHighlight.material = highlightMaterial;
                isHighlighted = true;
            }
            // --- FIM DO NOVO ---
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = false;
            promptUI?.Hide();

            // --- NOVO ---
            // Restaura o material original
            if (meshToHighlight != null && originalMaterial != null)
            {
                meshToHighlight.material = originalMaterial;
                isHighlighted = false;
            }
            // --- FIM DO NOVO ---
        }
    }
    
    void Update()
    {
        if (!isPlayerClose || npcData == null) return;
        
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            npcData.SerPurificado();
            this.enabled = false; // Se desliga
        }
        
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            npcData.SerMorto();
            this.enabled = false; // Se desliga
        }
    }
}