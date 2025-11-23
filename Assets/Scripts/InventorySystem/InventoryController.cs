// Nome do arquivo: InventoryController.cs
// CÓDIGO COMPLETO (FINALIZADO PARA PERSISTÊNCIA E CRIAÇÃO DE SLOTS)

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Necessário para Listas

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [Header("References")]
    public GameObject inventoryPanel;
    public Transform inventorySlotParent; // Onde os slots ficam

    // --- ADIÇÃO: CRIAÇÃO DE SLOTS ---
    [Tooltip("O prefab do slot individual para ser instanciado.")]
    public GameObject slotPrefab; 
    public int inventorySize = 12; // Define quantos slots o inventário terá
    // --- FIM DA ADIÇÃO ---

    private PlayerInputActions input;
    public bool IsInventoryOpen { get; private set; }

    private GameObject lastSelectedGameObject;
    private List<InventorySlot> slots = new List<InventorySlot>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // ESSENCIAL: Mantém o Controller ativo entre as cenas.
            DontDestroyOnLoad(gameObject);
        }
        IsInventoryOpen = false;
    }

    void OnEnable()
    {
        // Sempre que o InventoryManager avisar que mudou algo, atualizamos a UI
        InventoryManager.OnInventoryChanged += UpdateInventoryUI;
    }

    void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= UpdateInventoryUI;
    }

    void Start()
    {
        // 1. Checagem de Input (Permanece igual)
        if (InputManager.Instance == null)
        {
            Debug.LogError("InventoryController não encontrou o InputManager!");
            this.enabled = false;
            return;
        }
        input = InputManager.Instance.InputActions;
        //input.Player.Inventory.performed += OnInventoryPressed;
        //input.UI.Cancel.performed += OnCancelPressed;
        InputAction inventoryUIAction = input.FindAction("UI/Inventory");
        if (inventoryUIAction != null)
            inventoryUIAction.performed += OnCancelPressed;

        // 2. --- CRIAÇÃO DOS SLOTS ---
        if (inventorySlotParent == null || slotPrefab == null)
        {
            Debug.LogError("InventoryController não tem o Slot Prefab ou o Slot Parent configurado!");
            this.enabled = false;
            return;
        }

        // Cria e popula a lista de slots
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventorySlotParent);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot != null) 
            {
                slots.Add(slot);
                slotObj.SetActive(true); 
            }
        }
        // --- FIM DA CRIAÇÃO ---

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
            
        UpdateInventoryUI();
    }

    void OnDestroy()
    {
        if (input != null && InputManager.Instance != null)
        {
            //input.Player.Inventory.performed -= OnInventoryPressed;
            //input.UI.Cancel.performed -= OnCancelPressed;

            InputAction inventoryUIAction = input.FindAction("UI/Inventory");
            if (inventoryUIAction != null)
                inventoryUIAction.performed -= OnCancelPressed;
        }
    }

    public void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null) return;

        // Usa a lista 'slots' que criamos no Start()
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < InventoryManager.Instance.items.Count)
            {
                // Tem item: mostra ele
                var entry = InventoryManager.Instance.items[i];
                slots[i].SetItem(entry.item, entry.quantity);
            }
            else
            {
                // Não tem item: limpa o slot
                slots[i].SetItem(null, 0);
            }
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
            if (InventoryActionPanel.Instance != null) // <--- ADICIONE ISSO
            {
                InventoryActionPanel.Instance.HidePanel(); // <--- ADICIONE ISSO
            }
            UpdateInventoryUI();
            
            Time.timeScale = 0f;
            SetCursorLocked(false);
            InputManager.Instance.SwitchToUIMap();

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
        InventorySlot firstSlot = null;

        if (slots.Count > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Transform child = slots[i].transform;
                if (child != null && child.gameObject.activeSelf)
                {
                    firstButton = child.GetComponent<Button>();
                    firstSlot = child.GetComponent<InventorySlot>(); 

                    if (firstButton != null && firstSlot != null)
                        break;
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

        if (InventoryActionPanel.Instance != null)
        {
            InventoryActionPanel.Instance.ShowPanel(firstSlot);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}