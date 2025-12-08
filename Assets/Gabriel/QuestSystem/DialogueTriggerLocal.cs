using UnityEngine;

public class DialogueTriggerLocal : MonoBehaviour
{
    [TextArea] public string[] lines;
    public string speakerName = "Eu";

    // Chame esta função no UnityEvent do QuestEventTrigger
    public void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(lines, speakerName);
        }
    }
}