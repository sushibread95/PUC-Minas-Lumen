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
    private PlayerInputActions input; // <-- Será preenchido pelo InputManager
    private Transform currentTarget;

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    public string itemTag = "Item";
    public LayerMask itemLayer = ~0;
    public Material highlightMaterial;
    private Material originalMaterial;

    void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("ItemPickupSystem não encontrou o InputManager!");
            this.enabled = false; // Desabilita o script se não houver InputManager
            return;
        }
        input = InputManager.Instance.InputActions;
        
        input.Player.Interact.performed += OnInteractPressed;

        if (slotPrefab.activeSelf)
            slotPrefab.SetActive(false);

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventoryUIParent);
            slots.Add(slotObj.GetComponent<InventorySlot>());
            slotObj.SetActive(false);
        }

        if (InventoryManager.Instance != null)
            UpdateUIFromManager();
    }

    void OnDestroy()
    {
        // Se desinscreve para evitar erros
        if (input != null)
        {
            input.Player.Interact.performed -= OnInteractPressed;
        }
    }

    void Update()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            ClearHighlight(); // Limpa o highlight se pausar
            currentTarget = null;
            return;
        }

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
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (currentTarget == null) return;

        ObjectType objType = currentTarget.GetComponent<ObjectType>();
        if (objType == null || objType.TypeObjec == null) return;

        InventoryManager.Instance.AddItem(objType.TypeObjec);
        UpdateUIFromManager();

        Destroy(currentTarget.gameObject);
        ClearHighlight();
        currentTarget = null;
    }

    private void AddItem(Objects newItem)
    {
        foreach (var slot in slots)
        {
            if (slot.item != null && slot.item.objectName == newItem.objectName)
            {
                if (slot.quantity < newItem.maxStack)
                {
                    slot.quantity++;
                    slot.UpdateUI();
                    return;
                }
            }
        }
        foreach (var slot in slots)
        {
            if (slot.item == null)
            {
                slot.SetItem(newItem, 1);
                return;
            }
        }
        Debug.Log("Inventário cheio!");
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
        foreach (var slot in slots)
        {
            slot.item = null;
            slot.quantity = 0;
            slot.UpdateUI();
            slot.gameObject.SetActive(false);
        }

        if (InventoryManager.Instance == null) return;
        for (int i = 0; i < InventoryManager.Instance.items.Count && i < slots.Count; i++)
        {
            var entry = InventoryManager.Instance.items[i];
            slots[i].SetItem(entry.item, entry.quantity);
        }
    }
}