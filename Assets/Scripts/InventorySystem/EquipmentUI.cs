// Nome do arquivo: EquipmentUI.cs
// CÓDIGO COMPLETO (MODIFICADO PARA ENCONTRAR O PLAYER DINAMICAMENTE)

using UnityEngine;
using UnityEngine.InputSystem;

public class EquipmentUI : MonoBehaviour
{
    public GameObject equipmentPanel;
    // --- MODIFICAÇÃO (REMOÇÃO DA ATRIBUIÇÃO POR INSPECTOR) ---
    // A referência ao Player será encontrada dinamicamente.
    private PlayerControllerSystem playerController; 
    // --- FIM DA MODIFICAÇÃO ---

    private PlayerInputActions input;
    private bool equipmentActive = false;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Enable();
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
        // --- ADIÇÃO: ENCONTRA O PLAYER APÓS A CENA CARREGAR ---
        // Agora que este script é persistente, ele procura o Player que
        // é criado (spawnado) na cena de jogo.
        playerController = FindFirstObjectByType<PlayerControllerSystem>();
        // --- FIM DA ADIÇÃO ---
    }

    private void OnEquipmentPressed(InputAction.CallbackContext ctx)
    {
        ToggleEquipment();
    }

    private void ToggleEquipment()
    {
        equipmentActive = !equipmentActive;
        equipmentPanel.SetActive(equipmentActive);

        // O LockCursor é chamado aqui
        if (playerController != null)
        {
            playerController.LockCursor(!equipmentActive);
        }
    }
}