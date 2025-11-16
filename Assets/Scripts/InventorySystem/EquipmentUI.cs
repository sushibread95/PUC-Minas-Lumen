using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentUI : MonoBehaviour
{
    public GameObject equipmentPanel;
    public PlayerControllerSystem playerController; // Assumindo que você tem este script

    private PlayerInputActions input;
    private bool equipmentActive = false;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Enable();
        // !! IMPORTANTE !!
        // Você precisa criar esta Ação "Equipment" no seu Asset PlayerInputActions
        input.Player.Equipment.performed += OnEquipmentPressed;
    }

    void OnDisable()
    {
        input.Player.Equipment.performed -= OnEquipmentPressed;
        input.Disable();
    }

    void Start()
    {
        equipmentPanel.SetActive(false);
        // (Opcional: pegue o playerController se não estiver atribuído)
    }

    private void OnEquipmentPressed(InputAction.CallbackContext ctx)
    {
        ToggleEquipment();
    }

    private void ToggleEquipment()
    {
        equipmentActive = !equipmentActive;
        equipmentPanel.SetActive(equipmentActive);

        if (playerController != null)
        {
            // Trava o cursor se o menu fechar, destrava se abrir
            playerController.LockCursor(!equipmentActive);
        }
    }
}