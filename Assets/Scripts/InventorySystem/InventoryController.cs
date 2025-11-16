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
            StartCoroutine(SelectFirstSlotLater());

            if (InventoryActionPanel.Instance != null)
            {
                InventoryActionPanel.Instance.ShowPanel(null);
            }
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
        yield return null;
        SelectFirstAvailableSlot();
    }

    private void SelectFirstAvailableSlot()
    {
        if (EventSystem.current == null || inventorySlotParent == null)
            return;

        Button firstButton = null;
        if (inventorySlotParent.childCount > 0)
        {
            for (int i = 0; i < inventorySlotParent.childCount; i++)
            {
                Transform child = inventorySlotParent.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                {
                    firstButton = child.GetComponent<Button>();
                    if (firstButton != null) break;
                }
            }
        }

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
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}