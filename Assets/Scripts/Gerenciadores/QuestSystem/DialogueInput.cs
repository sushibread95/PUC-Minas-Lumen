using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInput : MonoBehaviour
{
void Update()
    {
        if (DialogueManager.Instance == null || 
            DialogueManager.Instance.dialoguePanel == null || 
            !DialogueManager.Instance.dialoguePanel.activeSelf) 
            return;

        bool wantsToAdvance = false;

        // CORREÇÃO: Segurança de Hardware!
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            wantsToAdvance = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wantsToAdvance = true;
        }

        if (wantsToAdvance)
        {
            DialogueManager.Instance.DisplayNextSentence();
        }
    }
    
    }