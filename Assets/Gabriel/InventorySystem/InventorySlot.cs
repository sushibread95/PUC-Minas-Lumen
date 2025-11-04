using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI itemNameText;
    public GameObject actionPanel;

    [HideInInspector] public Objects item;
    [HideInInspector] public int quantity;

    public void SetItem(Objects newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        gameObject.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (item == null)
        {
            gameObject.SetActive(false);
        }
        if (item != null)
        {

          
            if (iconImage != null && item.icon != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = item.icon;
                iconImage.preserveAspect = true;
            }

            if (itemNameText != null)
                itemNameText.text = item.objectName;

            quantityText.text = quantity > 1 ? quantity.ToString() : "";
        }
        else
        {
            if (iconImage != null)
                iconImage.enabled = false;

            if (itemNameText != null)
                itemNameText.text = "";

            quantityText.text = "";
            actionPanel.SetActive(false);
        }
    }

    public void OnSlotClicked()
    {
        if (item == null) return;
        actionPanel.SetActive(!actionPanel.activeSelf);
    }

    public void OnUseClicked()
    {
        Debug.Log($"Usou: {item.objectName}");
        actionPanel.SetActive(false);
    }

    public void OnEquipClicked()
    {
        Debug.Log($"Equipou: {item.objectName}");
        actionPanel.SetActive(false);
    }

    public void OnDropClicked()
    {
        Debug.Log($"Descartou: {item.objectName}");
        quantity--;
        if (quantity <= 0)
        {
            item = null;
        }
        UpdateUI();
        actionPanel.SetActive(false);
    }
}
