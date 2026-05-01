using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameInputListener : MonoBehaviour
{
    private InputAction uiInventoryAction;

    void Start()
    {
        if (InputManager.Instance != null)
        {
            SubscribeInputs();
        }
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            SubscribeInputs();
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            UnsubscribeInputs();
        }
    }

    void SubscribeInputs()
    {
        UnsubscribeInputs(); // Garante limpeza antes de assinar

        var actions = InputManager.Instance.InputActions;
        
        // 1. Mapa Player (Para ABRIR)
        actions.Player.Inventory.performed += OnInventoryPressed;
        actions.Player.Map.performed += OnMapPressed;

        // 2. Mapa UI (Para FECHAR) - A Correção Mágica
        // Usamos FindAction para evitar erros de compilação caso você não tenha criado a ação ainda
        uiInventoryAction = actions.UI.Get().FindAction("Inventory");
        if (uiInventoryAction != null)
        {
            uiInventoryAction.performed += OnInventoryPressed;
        }
        else
        {
            Debug.LogWarning("GameInputListener: Ação 'Inventory' não encontrada no Mapa 'UI'. Adicione-a no Input Actions Asset para fechar o menu com a mesma tecla.");
        }
    }

    void UnsubscribeInputs()
    {
        if (InputManager.Instance == null) return;

        var actions = InputManager.Instance.InputActions;
        
        actions.Player.Inventory.performed -= OnInventoryPressed;
        actions.Player.Map.performed -= OnMapPressed;

        if (uiInventoryAction != null)
        {
            uiInventoryAction.performed -= OnInventoryPressed;
            uiInventoryAction = null;
        }
    }

    private void OnInventoryPressed(InputAction.CallbackContext ctx)
    {
        // Trava de segurança para não abrir inventário no MainMenu
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        if (CharacterMenuWindow.Instance == null) return;

        // A lógica de Toggle já cuida de Abrir/Fechar
        if (!CharacterMenuWindow.Instance.IsMenuOpen)
        {
            CharacterMenuWindow.Instance.OpenSpecificTab(0); 
            CharacterMenuWindow.Instance.ToggleMenu(); 
        }
        else
        {
            CharacterMenuWindow.Instance.ToggleMenu(); 
        }
    }

    private void OnMapPressed(InputAction.CallbackContext ctx)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        if (CharacterMenuWindow.Instance == null) return;

        // Nota: Se quiser fechar o mapa com 'M', precisa fazer o mesmo processo (criar ação Map no UI)
        if (!CharacterMenuWindow.Instance.IsMenuOpen)
        {
            CharacterMenuWindow.Instance.OpenSpecificTab(2); 
            CharacterMenuWindow.Instance.ToggleMenu();
        }
        else
        {
            CharacterMenuWindow.Instance.ToggleMenu();
        }
    }
}