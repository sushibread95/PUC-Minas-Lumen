using UnityEngine;
using UnityEngine.InputSystem;

public class QuickSlotInput : MonoBehaviour
{
    public GameObject quickSlotPanel;
    private PlayerInputActions input;

    void Awake()
    {
        // Precisamos pegar o input do Start()
    }

// Substitua o seu Start e EnableInputs por isto:
    void Start()
    {
        // Vazio. Deixe o OnEnable fazer o trabalho de ligar os inputs.
    }

    void EnableInputs()
    {
        if (input == null) return;

        DisableInputs(); // <-- SEGURANÇA: Garante que nunca haverá inscrição dupla!

        input.Player.QuickSlot1.performed += OnQuickSlot1;
        input.Player.QuickSlot2.performed += OnQuickSlot2;
        input.Player.QuickSlot3.performed += OnQuickSlot3;
        input.Player.QuickSlot4.performed += OnQuickSlot4;
        input.Player.ToggleQuickSlots.performed += TogglePanel;
    }
    void OnEnable()
    {
        EnableInputs();
    }

    void OnDisable()
    {
        DisableInputs();
    }

    void DisableInputs()
    {
        if (input == null) return;

        input.Player.QuickSlot1.performed -= OnQuickSlot1;
        input.Player.QuickSlot2.performed -= OnQuickSlot2;
        input.Player.QuickSlot3.performed -= OnQuickSlot3;
        input.Player.QuickSlot4.performed -= OnQuickSlot4;
        input.Player.ToggleQuickSlots.performed -= TogglePanel;
    }

    // Fun��es separadas para garantir que o 'unsubscribe' funcione
    private void OnQuickSlot1(InputAction.CallbackContext ctx) { UseQuickSlot(0); }
    private void OnQuickSlot2(InputAction.CallbackContext ctx) { UseQuickSlot(1); }
    private void OnQuickSlot3(InputAction.CallbackContext ctx) { UseQuickSlot(2); }
    private void OnQuickSlot4(InputAction.CallbackContext ctx) { UseQuickSlot(3); }

    void UseQuickSlot(int index)
    {
        if (InventoryManager.Instance == null) return;
        Objects item = InventoryManager.Instance.quickSlots[index];
        if (item != null)
        {
            // Mude de PlayerStats.Instance para HealthSystem.Instance
            InventoryManager.Instance.UseItem(item, HealthSystem.Instance.gameObject);
        }
    }

    void TogglePanel(InputAction.CallbackContext ctx)
    {
        if (quickSlotPanel != null)
        {
            quickSlotPanel.SetActive(!quickSlotPanel.activeSelf);
        }
    }
}