using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class ItemPickupSystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 8;
    public Transform inventoryUIParent; // Arraste o GridPanel aqui
    public GameObject slotPrefab;       // Arraste o Prefab do Slot1 aqui

    private List<InventorySlot> slots = new List<InventorySlot>();
    private PlayerInputActions input;
    private Transform currentTarget;

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    public string itemTag = "Item";
    public LayerMask itemLayer = ~0;
    public Material highlightMaterial;
    private Material originalMaterial;

    void Start()
    {
        // Integração com InputManager
        if (InputManager.Instance == null)
        {
            Debug.LogError("ItemPickupSystem não encontrou o InputManager!");
            this.enabled = false;
            return;
        }
        input = InputManager.Instance.InputActions;
        input.Player.Interact.performed += OnInteractPressed;
        
        // Lógica original de UI
        if (slotPrefab == null || inventoryUIParent == null)
        {
            Debug.LogError("ItemPickupSystem: 'Slot Prefab' ou 'Inventory UI Parent' não estão configurados no Inspector!");
            return;
        }

        if (slotPrefab.activeSelf)
            slotPrefab.SetActive(false);

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventoryUIParent);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slots.Add(slot);
                slotObj.SetActive(false);
            }
        }

        if (InventoryManager.Instance != null)
            UpdateUIFromManager();
    }

    void OnDestroy()
    {
        if (input != null && InputManager.Instance != null)
        {
            input.Player.Interact.performed -= OnInteractPressed;
        }
    }

    void Update()
    {
        // "Guarda Mestra" para todos os menus
        // CORREÇÃO: (Erro CS0103) 'StealthUI' não existe. Comentado e removido o '||' da linha anterior.
        if (input == null ||
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen) ||
           (ChoiceUI.Instance != null && ChoiceUI.Instance.gameObject.activeInHierarchy)) // Guarda futura
           // (StealthUI.Instance != null && StealthUI.Instance.gameObject.activeInHierarchy)) // Guarda futura
        {
            ClearHighlight(); // Limpa o highlight se o jogo pausar
            return;
        }
        
        DetectItemInFront();
    }

    private void DetectItemInFront()
    {
        // Lógica original
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
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        // Guarda extra
        if (currentTarget == null || 
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
        {
            return;
        }

        ObjectType objType = currentTarget.GetComponent<ObjectType>();
        if (objType == null || objType.TypeObjec == null) return;

        // 1. Adiciona ao "cérebro"
        InventoryManager.Instance.AddItem(objType.TypeObjec);

        // 2. Registra no "Save" se for um item de cena (MODIFICAÇÃO NECESSÁRIA)
        if (!string.IsNullOrEmpty(objType.itemInstanceID))
        {
            if(WorldStateManager.Instance != null)
                WorldStateManager.Instance.RegisterCollectedItem(objType.itemInstanceID);
        }

        // 3. Atualiza UI
        UpdateUIFromManager();

        // 4. Destrói o objeto
        Destroy(currentTarget.gameObject);
        ClearHighlight();
    }
    
    // --- LÓGICA ORIGINAL DO SEU COLEGA (SEM MUDANÇAS) ---
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
            originalMaterial = null;
        }
        currentTarget = null;
    }
    
    private void UpdateUIFromManager()
    {
        // Preenche os slots com base no InventoryManager
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < InventoryManager.Instance.items.Count)
            {
                // Se temos um item para este slot
                var entry = InventoryManager.Instance.items[i];
                slots[i].SetItem(entry.item, entry.quantity);
            }
            else
            {
                // Se não temos item, limpa o slot
                slots[i].item = null;
                slots[i].quantity = 0;
                slots[i].UpdateUI();
            }
        }
    }
}