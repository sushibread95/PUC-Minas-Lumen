using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class ItemPickupSystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 8;
    public Transform inventoryUIParent;
    public GameObject slotPrefab;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private PlayerInputActions input;
    private Transform currentTarget;

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    public string itemTag = "Item";
    public LayerMask itemLayer = ~0;
    public Material highlightMaterial;
    private Material originalMaterial;

    void Awake()
    {
        // input = new PlayerInputActions(); // REMOVIDO
    }

    void OnEnable()
    {
        // input.Enable(); // REMOVIDO

        // Movido para o Start() para garantir que 'input' não seja nulo
        // if (input != null)
        // {
        //     input.Player.Interact.performed += OnInteractPressed;
        // }

        InventoryManager.OnInventoryChanged += UpdateUIFromManager;
    }

    void OnDisable()
    {
        if (input != null)
        {
            input.Player.Interact.performed -= OnInteractPressed;
        }
        // input.Disable(); // REMOVIDO

        InventoryManager.OnInventoryChanged -= UpdateUIFromManager;
    }

    void Start()
    {
        // --- INÍCIO DA MODIFICAÇÃO ---
        if (InputManager.Instance == null)
        {
            Debug.LogError("ItemPickupSystem não encontrou o InputManager! A coleta de itens não vai funcionar.");
            this.enabled = false;
            return;
        }
        input = InputManager.Instance.InputActions;
        input.Player.Interact.performed += OnInteractPressed;
        // --- FIM DA MODIFICAÇÃO ---

        if (slotPrefab.activeSelf)
            slotPrefab.SetActive(false);

        foreach (Transform child in inventoryUIParent)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventoryUIParent);
            slots.Add(slotObj.GetComponent<InventorySlot>());
            slotObj.SetActive(false);
        }

        UpdateUIFromManager();
    }

    void Update()
    {
        DetectItemInFront();
    }

    private void DetectItemInFront()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, itemLayer))
        {
            if (hit.collider.CompareTag(itemTag))
            {
                if (currentTarget != hit.transform)
                {
                    ClearHighlight();
                    currentTarget = hit.transform;
                    HighlightItem(currentTarget);
                }
                return;
            }
        }

        ClearHighlight();
        currentTarget = null;
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (currentTarget == null) return;

        ObjectType objType = currentTarget.GetComponent<ObjectType>();
        if (objType == null || objType.TypeObjec == null) return;

        InventoryManager.Instance.AddItem(objType.TypeObjec);

        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(objType.id))
        {
            WorldStateManager.Instance.RegisterCollectedItem(objType.id);
        }
        else
        {
            Debug.LogWarning($"Item {objType.name} não tem ID ou WorldStateManager não foi encontrado. Não será salvo.");
        }

        Destroy(currentTarget.gameObject);
        ClearHighlight();
        currentTarget = null;
    }

    private void HighlightItem(Transform item)
    {
        Renderer rend = item.GetComponentInChildren<Renderer>();
        if (rend != null && highlightMaterial != null)
        {
            originalMaterial = rend.material;
            rend.material = highlightMaterial;
        }
    }

    private void ClearHighlight()
    {
        if (currentTarget == null) return;
        Renderer rend = currentTarget.GetComponentInChildren<Renderer>();
        if (rend != null && originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }

    private void UpdateUIFromManager()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogWarning($"ItemPickupSystem: Slot {i} na lista é nulo. Foi destruído?");
                continue;
            }

            if (i < InventoryManager.Instance.items.Count)
            {
                var entry = InventoryManager.Instance.items[i];
                slots[i].SetItem(entry.item, entry.quantity);
            }
            else
            {
                slots[i].SetItem(null, 0);
            }
        }
    }
}