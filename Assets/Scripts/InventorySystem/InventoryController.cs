using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [Header("References")]
    public GameObject inventoryPanel;
    public Transform inventorySlotParent;

    private PlayerInputActions input;
    public bool IsInventoryOpen { get; private set; }

    private GameObject lastSelectedGameObject;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        IsInventoryOpen = false;
    }

    void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InventoryController não encontrou o InputManager!");
            this.enabled = false;
            return;
        }

        input = InputManager.Instance.InputActions;

        input.Player.Inventory.performed += OnInventoryPressed;
        input.UI.Cancel.performed += OnCancelPressed;

        InputAction inventoryUIAction = input.FindAction("UI/Inventory");
        if (inventoryUIAction != null)
            inventoryUIAction.performed += OnCancelPressed;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (input != null && InputManager.Instance != null)
        {
            input.Player.Inventory.performed -= OnInventoryPressed;
            input.UI.Cancel.performed -= OnCancelPressed;

            InputAction inventoryUIAction = input.FindAction("UI/Inventory");
            if (inventoryUIAction != null)
                inventoryUIAction.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        if (!IsInventoryOpen || inventoryPanel == null || !inventoryPanel.activeSelf) return;

        if (EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject == null &&
               (Mouse.current != null && !Mouse.current.delta.IsActuated(0.1f)))
            {
                if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else
                {
                    SelectFirstAvailableSlot();
                }
            }
            else if (EventSystem.current.currentSelectedGameObject != null)
            {
                lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
            }
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext ctx)
    {
        if (IsInventoryOpen)
        {
            ToggleInventory();
        }
    }

    private void OnInventoryPressed(InputAction.CallbackContext ctx)
    {
        if ((PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
            (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy))
        {
            return;
        }

        if (!IsInventoryOpen)
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);

        if (IsInventoryOpen)
        {
            Time.timeScale = 0f;
            SetCursorLocked(false);
            InputManager.Instance.SwitchToUIMap();

            // Esta corrotina agora faz todo o trabalho
            StartCoroutine(SelectFirstSlotLater());
        }
        else
        {
            Time.timeScale = 1f;
            SetCursorLocked(true);
            InputManager.Instance.SwitchToGameplayMap();

            if (InventoryActionPanel.Instance != null)
            {
                InventoryActionPanel.Instance.HidePanel();
            }

            if (QuickSlotAssignmentUI.Instance != null)
            {
                QuickSlotAssignmentUI.Instance.HidePanel();
            }

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            lastSelectedGameObject = null;
        }
    }

    private IEnumerator SelectFirstSlotLater()
    {
        // Espera 1 frame. Isso dá tempo para o IventoryCanva
        // ligar e rodar o Awake() do ActionPanel.
        yield return null;

        // Agora que o ActionPanel.Instance existe,
        // esta função pode chamá-lo.
        SelectFirstAvailableSlot();
    }

    // --- FUNÇÃO MODIFICADA ---
    private void SelectFirstAvailableSlot()
    {
        if (EventSystem.current == null || inventorySlotParent == null)
            return;

        Button firstButton = null;
        InventorySlot firstSlot = null; // --- LINHA ADICIONADA ---

        if (inventorySlotParent.childCount > 0)
        {
            for (int i = 0; i < inventorySlotParent.childCount; i++)
            {
                Transform child = inventorySlotParent.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                {
                    firstButton = child.GetComponent<Button>();
                    firstSlot = child.GetComponent<InventorySlot>(); // --- LINHA ADICIONADA ---

                    if (firstButton != null && firstSlot != null)
                        break;
                }
            }
        }

        // 1. Seleciona o slot para o Gamepad
        if (firstButton != null && firstButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            lastSelectedGameObject = firstButton.gameObject;
        }
        else
        {
            lastSelectedGameObject = null;
        }

        // 2. ATIVA O ACTION PANEL com as infos do primeiro slot
        // --- BLOCO ADICIONADO ---
        if (InventoryActionPanel.Instance != null)
        {
            // Se firstSlot for null (inventário vazio), 
            // o painel vai abrir com a msg "Selecione um item".
            InventoryActionPanel.Instance.ShowPanel(firstSlot);
        }
        else
        {
            Debug.LogError("InventoryActionPanel.Instance ainda é NULO! Verifique a hierarquia.");
        }
        // --- FIM DO BLOCO ---
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}