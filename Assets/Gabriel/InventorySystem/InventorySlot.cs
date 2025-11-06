using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data (Do not set in prefab)")]
    public Objects item;
    public int quantity;

    [Header("UI References (Set in prefab)")]
    public Image icon;
    public TextMeshProUGUI quantityText;

    public void SetItem(Objects newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (item != null)
        {
            gameObject.SetActive(true);
            icon.sprite = item.icon;
            icon.enabled = true;
            
            if (quantity > 1 && item.maxStack > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
        else
        {
            gameObject.SetActive(false);
            item = null;
            quantity = 0;
            icon.enabled = false;
            quantityText.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null && ItemDescriptionUI.Instance != null)
        {
            ItemDescriptionUI.Instance.ShowDescription(item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemDescriptionUI.Instance != null)
        {
            ItemDescriptionUI.Instance.HideDescription();
        }
    }
}