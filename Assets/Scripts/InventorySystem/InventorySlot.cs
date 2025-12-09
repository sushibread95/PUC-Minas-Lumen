// Nome do arquivo: InventorySlot.cs
// CÓDIGO COMPLETO (MODIFICADO PARA NÃO DESAPARECER QUANDO VAZIO)

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

    // --- MODIFICAÇÃO: Lógica de Exibição ---
    public void SetItem(Objects newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;

        // ANTES: Se item == null, desativava o objeto (gameObject.SetActive(false))
        // AGORA: Mantemos o objeto ATIVO sempre, para ele ocupar espaço na grade.
        gameObject.SetActive(true); 
        
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (item != null)
        {
            // Tem item: Mostra ícone e texto
            iconImage.enabled = true;
            iconImage.sprite = item.icon;
            iconImage.preserveAspect = true;
            
            if (itemNameText != null) itemNameText.text = item.objectName;
            
            // Só mostra número se for pilha > 1
            if (quantityText != null) 
                quantityText.text = quantity > 1 ? quantity.ToString() : "";
        }
        else
        {
            // Não tem item (Vazio): Esconde ícone, mas o SLOT continua visível (fundo)
            iconImage.enabled = false; 
            iconImage.sprite = null;
            
            if (itemNameText != null) itemNameText.text = "";
            if (quantityText != null) quantityText.text = "";
        }
    }
    // --- FIM DA MODIFICAÇÃO ---

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
        
        if (DragDropIcon.Instance != null)
            DragDropIcon.Instance.ShowIcon(item.icon);
            
        if (iconImage != null)
            iconImage.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggedSlot = null;
        
        if (DragDropIcon.Instance != null)
            DragDropIcon.Instance.HideIcon();
            
        if (iconImage != null)
            iconImage.color = new Color(1, 1, 1, 1f);
    }
}