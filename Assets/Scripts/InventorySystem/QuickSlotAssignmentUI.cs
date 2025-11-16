using UnityEngine;
using UnityEngine.UI;

public class QuickSlotAssignmentUI : MonoBehaviour
{
    public static QuickSlotAssignmentUI Instance { get; private set; }

    [Header("Referências")]
    public GameObject panelObject;
    public Image[] slotIcons;
    public Button[] slotButtons;

    private Objects itemToAssign;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => OnSlotSelected(index));
        }

        if (panelObject == null) panelObject = this.gameObject;

        // CORREÇÃO: Esconde o painel *visual* no Awake.
        panelObject.SetActive(false);
    }

    void OnEnable()
    {
        InventoryManager.OnQuickSlotsChanged += UpdateIcons;
    }

    void OnDisable()
    {
        InventoryManager.OnQuickSlotsChanged -= UpdateIcons;
    }

    public void StartAssignment(Objects item)
    {
        this.itemToAssign = item;
        panelObject.SetActive(true);
        UpdateIcons();
    }

    public void OnSlotSelected(int index)
    {
        if (itemToAssign == null) return;
        InventoryManager.Instance.AssignQuickSlot(index, itemToAssign);
        itemToAssign = null;
        panelObject.SetActive(false);
    }

    private void UpdateIcons()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i >= InventoryManager.Instance.quickSlots.Length) break;
            Objects itemInSlot = InventoryManager.Instance.quickSlots[i];
            if (itemInSlot != null)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = itemInSlot.icon;
            }
            else
            {
                slotIcons[i].enabled = false;
                slotIcons[i].sprite = null;
            }
        }
    }
    public void HidePanel()
    {
        itemToAssign = null;
        if (panelObject != null)
            panelObject.SetActive(false);
    }
}