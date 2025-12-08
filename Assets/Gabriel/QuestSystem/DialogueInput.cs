using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueInput : MonoBehaviour
{
    void Update()
    {
        // Se o painel de diálogo não estiver ativo, ignora
        if (DialogueManager.Instance == null || 
            DialogueManager.Instance.dialoguePanel == null || 
            !DialogueManager.Instance.dialoguePanel.activeSelf) 
            return;

        // Verifica inputs genéricos para avançar (Mouse Click, Enter, Espaço)
        // Nota: Como estamos no mapa de UI, o Input System já lida com cliques de UI, 
        // mas isso garante avanço por teclado também.
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame || 
            Mouse.current.leftButton.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            DialogueManager.Instance.DisplayNextSentence();
        }
    }
}