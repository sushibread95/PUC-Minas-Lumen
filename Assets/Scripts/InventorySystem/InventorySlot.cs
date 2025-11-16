using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References (Limpo)")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI itemNameText;

    [HideInInspector] public Objects item;
    [HideInInspector] public int quantity;

    public static InventorySlot draggedSlot;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnSlotClicked);
        }
    }

    public void SetItem(Objects newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        if (item == null) gameObject.SetActive(false);
        else
        {
            gameObject.SetActive(true);
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (item != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = item.icon;
            iconImage.preserveAspect = true;
            if (itemNameText != null) itemNameText.text = item.objectName;
            quantityText.text = quantity > 1 ? quantity.ToString() : "";
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            if (itemNameText != null) itemNameText.text = "";
            quantityText.text = "";
        }
    }

    public void OnSlotClicked()
    {
        if (item == null) return;

        if (InventoryActionPanel.Instance != null)
            InventoryActionPanel.Instance.ShowPanel(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null)
        {
            eventData.pointerDrag = null;
            return;
        }
        draggedSlot = this;
        DragDropIcon.Instance.ShowIcon(item.icon);
        iconImage.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggedSlot = null;
        DragDropIcon.Instance.HideIcon();
        iconImage.color = new Color(1, 1, 1, 1f);
    }
}