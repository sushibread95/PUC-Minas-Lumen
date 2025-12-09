using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [Header("References")]
    public GameObject inventoryPanel; // O painel interno (dentro da aba)
    public Transform inventorySlotParent; 

    [Header("Settings")]
    public GameObject slotPrefab; 
    public int inventorySize = 12;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private GameObject lastSelectedGameObject;

    // Propriedade para checar se está visível (baseado no painel)
    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeInHierarchy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnEnable()
    {
        InventoryManager.OnInventoryChanged += UpdateInventoryUI;
    }

    void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= UpdateInventoryUI;
    }

    void Start()
    {
        // Criação dos Slots (Igual ao anterior)
        if (inventorySlotParent == null || slotPrefab == null)
        {
            Debug.LogError("InventoryController: Faltam referências!");
            this.enabled = false;
            return;
        }

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, inventorySlotParent);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot != null) 
            {
                slots.Add(slot);
                slotObj.SetActive(true); 
            }
        }

        UpdateInventoryUI();
    }

    // --- FUNÇÃO CHAMADA PELO CharacterMenuWindow ---
    public void OnInventoryTabOpened()
    {
        UpdateInventoryUI();
        // Força a seleção do primeiro slot para controle/teclado
        StartCoroutine(SelectFirstSlotLater());
    }
    // -----------------------------------------------

    public void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
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

    void Update()
    {
        // Só roda lógica de seleção se o inventário estiver visível
        if (!IsInventoryOpen) return;

        // Lógica de manter seleção (Gamepad/Teclado)
        if (EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject == null &&
               (Mouse.current != null && !Mouse.current.delta.IsActuated(0.1f)))
            {
                if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else
                {
                    SelectFirstAvailableSlot();
                }
            }
            else if (EventSystem.current.currentSelectedGameObject != null)
            {
                lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
            }
        }
    }

    private IEnumerator SelectFirstSlotLater()
    {
        yield return null;
        SelectFirstAvailableSlot();
    }

    private void SelectFirstAvailableSlot()
    {
        if (EventSystem.current == null || inventorySlotParent == null) return;

        // Tenta achar o primeiro botão ativo nos slots
        Button firstButton = inventorySlotParent.GetComponentInChildren<Button>();

        if (firstButton != null && firstButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            lastSelectedGameObject = firstButton.gameObject;
        }
    }
}