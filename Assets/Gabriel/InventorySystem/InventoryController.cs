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
    public Button defaultSelectedButton; 

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

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (input != null && InputManager.Instance != null)
        {
            input.Player.Inventory.performed -= OnInventoryPressed;
            input.UI.Cancel.performed -= OnCancelPressed;
        }
    }

    void Update()
    {
        if (!IsInventoryOpen || inventoryPanel == null || !inventoryPanel.activeSelf) return;
        if (EventSystem.current != null)
        {
             if (EventSystem.current.currentSelectedGameObject == null && (Mouse.current != null && !Mouse.current.delta.IsActuated(0.1f)))
             {
                 if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy)
                 {
                     EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                 }
                 else if (defaultSelectedButton != null && defaultSelectedButton.interactable)
                 {
                     EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
                     lastSelectedGameObject = defaultSelectedButton.gameObject;
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
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            return;
        }
        if (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy)
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

            if (defaultSelectedButton != null)
            {
                 StartCoroutine(SelectButtonLater(defaultSelectedButton));
                 lastSelectedGameObject = defaultSelectedButton.gameObject;
            }
        }
        else
        {
            Time.timeScale = 1f;
            SetCursorLocked(true);
            InputManager.Instance.SwitchToGameplayMap(); 

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            lastSelectedGameObject = null;
        }
    }
    
    private IEnumerator SelectButtonLater(Button button)
    {
        yield return null; 
        if (button != null && EventSystem.current != null && button.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}