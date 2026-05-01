using UnityEngine;

public class DialogueTriggerLocal : MonoBehaviour
{
    [TextArea] public string[] lines;
    public string speakerName = "Eu";
    
    [Header("Configuração")]
    public bool triggerOnlyOnce = true; // Opção para não repetir toda hora
    private bool hasTriggered = false;

    // --- A PARTE QUE FALTAVA ---
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se foi o Player que entrou (e não um inimigo ou parede)
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            TriggerDialogue();
            hasTriggered = true;
        }
    }
    // ---------------------------

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(lines, speakerName);
        }
    }
}