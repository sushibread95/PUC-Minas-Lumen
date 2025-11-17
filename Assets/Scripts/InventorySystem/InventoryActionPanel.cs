using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryActionPanel : MonoBehaviour
{
    public static InventoryActionPanel Instance { get; private set; }

    [Header("Item Details")]
    public Image itemIcon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;

    [Header("Referências dos Botões")]
    public Button useButton;
    public Button equipButton;
    public Button dropButton;
    public Button assignQuickSlotButton;

    [Header("Configuração")]
    public GameObject panelObject;

    private InventorySlot currentSlot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Limpe os 'On Click()' do Inspector!
        // O script cuida disso aqui:
        useButton.onClick.RemoveAllListeners(); // Segurança extra
        useButton.onClick.AddListener(OnUse);

        equipButton.onClick.RemoveAllListeners(); // Segurança extra
        equipButton.onClick.AddListener(OnEquip);

        dropButton.onClick.RemoveAllListeners(); // Segurança extra
        dropButton.onClick.AddListener(OnDrop);

        assignQuickSlotButton.onClick.RemoveAllListeners(); // Segurança extra
        assignQuickSlotButton.onClick.AddListener(OnAssignQuickSlot);

        if (panelObject == null) panelObject = this.gameObject;

        panelObject.SetActive(false);
    }

    // Mostra o painel (chamado ao abrir o inventário)
    // ou ATUALIZA o painel (chamado ao clicar num slot)
    public void ShowPanel(InventorySlot slot)
    {
        if (panelObject != null && !panelObject.activeSelf)
            panelObject.SetActive(true);

        if (slot == null)
        {
            ClearDetails(); // Limpa e mostra "Selecione um Item"
            currentSlot = null;
        }
        else
        {
            currentSlot = slot;
            itemIcon.sprite = currentSlot.item.icon;
            itemName.text = currentSlot.item.objectName;
            itemDescription.text = currentSlot.item.description;
            itemIcon.enabled = true;

            useButton.gameObject.SetActive(currentSlot.item.useEffects.Count > 0);
            equipButton.gameObject.SetActive(currentSlot.item.isEquippable);
            dropButton.gameObject.SetActive(true);
            assignQuickSlotButton.gameObject.SetActive(currentSlot.item.isConsumable);
        }
    }

    public void HidePanel()
    {
        ClearDetails();
        currentSlot = null;
        if (panelObject != null)
            panelObject.SetActive(false);
    }

    private void ClearDetails()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        if (itemName != null) itemName.text = "Selecione um Item"; // Mensagem padrão
        if (itemDescription != null) itemDescription.text = "";

        useButton.gameObject.SetActive(false);
        equipButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(false);
        assignQuickSlotButton.gameObject.SetActive(false);
    }

    // --- LÓGICA DE BOTÕES ATUALIZADA ---

    private void OnUse()
    {
        if (currentSlot == null || currentSlot.item == null) return;

        // Mude de PlayerStats.Instance para HealthSystem.Instance
        InventoryManager.Instance.UseItem(currentSlot.item, HealthSystem.Instance.gameObject);

        ShowPanel(null);
    }

    private void OnEquip()
    {
        if (currentSlot == null || currentSlot.item == null) return;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.EquipItem(currentSlot.item);
        InventoryManager.Instance.RemoveItem(currentSlot.item);

        // CORREÇÃO: Limpa o painel, mas não o fecha.
        ShowPanel(null);
    }

    private void OnDrop()
    {
        if (currentSlot == null || currentSlot.item == null) return;
        InventoryManager.Instance.RemoveItem(currentSlot.item);

        // CORREÇÃO: Limpa o painel, mas não o fecha.
        ShowPanel(null);
    }

    private void OnAssignQuickSlot()
    {
        if (currentSlot == null || currentSlot.item == null) return;
        QuickSlotAssignmentUI.Instance.StartAssignment(currentSlot.item);

        // (Este botão não fecha o painel, como já havíamos feito)
    }
}