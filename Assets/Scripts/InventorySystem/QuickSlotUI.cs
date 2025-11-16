using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class QuickSlotUI : MonoBehaviour, IDropHandler
{
    [Header("Configuração")]
    public int slotIndex;

    [Header("Referências da UI")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI keybindText;

    void Start()
    {
        if (keybindText != null)
        {
            keybindText.text = (slotIndex + 1).ToString();
        }
        UpdateUI();
    }

    void OnEnable()
    {
        InventoryManager.OnInventoryChanged += UpdateUI;
        InventoryManager.OnQuickSlotsChanged += UpdateUI;
    }

    void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= UpdateUI;
        InventoryManager.OnQuickSlotsChanged -= UpdateUI;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot = InventorySlot.draggedSlot;
        if (draggedSlot != null && draggedSlot.item != null)
        {
            InventoryManager.Instance.AssignQuickSlot(slotIndex, draggedSlot.item);
        }
    }

    void UpdateUI()
    {
        if (InventoryManager.Instance == null) return;

        Objects item = InventoryManager.Instance.quickSlots[slotIndex];

        if (item != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = item.icon;

            int quantityInInventory = 0;
            var itemEntry = InventoryManager.Instance.items.Find(x => x.item == item);
            if (itemEntry != null)
            {
                quantityInInventory = itemEntry.quantity;
            }

            if (quantityInInventory > 1)
            {
                quantityText.text = quantityInInventory.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }

            if (quantityInInventory <= 0)
            {
                InventoryManager.Instance.AssignQuickSlot(slotIndex, null);
            }
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            quantityText.enabled = false;
        }
    }
}