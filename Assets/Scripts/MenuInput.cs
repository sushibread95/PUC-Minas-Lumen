using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInput : MonoBehaviour
{
    private PlayerInputActions input;

    void Start()
    {
        if (InputManager.Instance != null)
        {
            input = InputManager.Instance.InputActions;
            // Certifique-se de criar a action "Map" ou "Menu" no Input Actions
            input.Player.Map.performed += ctx => CharacterMenuWindow.Instance.ToggleMenu();
            // input.UI.Cancel.performed += ctx => CharacterMenuWindow.Instance.ToggleMenu(); // Opcional: Fechar com ESC
        }
    }
}