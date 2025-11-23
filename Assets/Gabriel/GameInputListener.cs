using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputListener : MonoBehaviour
{
    private PlayerInputActions input;

    void Awake()
    {
        // Inicializa
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Enable();
        // Se inscreve no evento "Inventory" (que você configurou para TAB)
        input.Player.Inventory.performed += OnInventoryPressed;
        // Se inscreve no "Map" (caso tenha)
        input.Player.Map.performed += OnMapPressed;
    }

    void OnDisable()
    {
        input.Player.Inventory.performed -= OnInventoryPressed;
        input.Player.Map.performed -= OnMapPressed;
        input.Disable();
    }

    private void OnInventoryPressed(InputAction.CallbackContext ctx)
    {
        if (CharacterMenuWindow.Instance == null) return;

        // Se o menu estiver fechado: Abre direto na aba 0 (Inventário)
        if (!CharacterMenuWindow.Instance.menuPanel.activeSelf)
        {
            CharacterMenuWindow.Instance.OpenSpecificTab(0); // 0 = Inventário
            CharacterMenuWindow.Instance.ToggleMenu(); // Liga o menu
        }
        // Se o menu já estiver aberto: Fecha
        else
        {
            CharacterMenuWindow.Instance.ToggleMenu(); // Desliga
        }
    }

    private void OnMapPressed(InputAction.CallbackContext ctx)
    {
        if (CharacterMenuWindow.Instance == null) return;

        if (!CharacterMenuWindow.Instance.menuPanel.activeSelf)
        {
            CharacterMenuWindow.Instance.OpenSpecificTab(2); // 2 = Mapa (exemplo)
            CharacterMenuWindow.Instance.ToggleMenu();
        }
        else
        {
            CharacterMenuWindow.Instance.ToggleMenu();
        }
    }
}