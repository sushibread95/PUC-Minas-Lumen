using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public static event System.Action OnInventoryChanged;
    public static event System.Action OnQuickSlotsChanged;

    [System.Serializable]
    public class InventoryItem
    {
        public Objects item;
        public int quantity;
        public InventoryItem(Objects newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public List<string> itemNames = new List<string>();
        public List<int> itemQuantities = new List<int>();
        public List<string> quickSlotItemNames = new List<string>();
    }

    public List<InventoryItem> items = new List<InventoryItem>();
    [Header("Quick Slots")]
    public Objects[] quickSlots = new Objects[4];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(Objects newItem)
    {
        InventoryItem entry = items.Find(x => x.item == newItem);
        if (entry != null)
        {
            if (entry.quantity < newItem.maxStack)
                entry.quantity++;
            else
                return;
        }
        else
        {
            items.Add(new InventoryItem(newItem, 1));
        }

        // --- MODIFICAÇÃO: Notifica o QuestSystem que um item foi coletado ---
        // Usa o nome do item como ID. Certifique-se que o 'targetID' na QuestDefinition seja igual ao nome do item.
        GameEvents.TriggerItemObtained(newItem.name, 1);
        // -------------------------------------------------------------------

        OnInventoryChanged?.Invoke();
    }

public void RemoveItem(Objects itemToRemove)
    {
        InventoryItem entry = items.Find(x => x.item == itemToRemove);
        if (entry != null)
        {
            entry.quantity--;
            if (entry.quantity <= 0)
            {
                items.Remove(entry);
                
                bool quickSlotChanged = false;
                for (int i = 0; i < quickSlots.Length; i++)
                {
                    if (quickSlots[i] == itemToRemove)
                    {
                        quickSlots[i] = null;
                        quickSlotChanged = true;
                    }
                }
                if (quickSlotChanged)
                {
                    OnQuickSlotsChanged?.Invoke();
                }
            }
            
            OnInventoryChanged?.Invoke();
        }
        
    }

    public bool HasItem(Objects itemToCheck)
    {
        if (itemToCheck == null) return false;

        // Procura na lista de itens
        InventoryItem entry = items.Find(x => x.item == itemToCheck);

        // Retorna true se encontrou
        return entry != null;
    }

    public void UseItem(Objects itemToUse, GameObject user)
    {
        InventoryItem entry = items.Find(x => x.item == itemToUse);
        if (entry == null) return;
        if (user == null) return;
        foreach (var effect in itemToUse.useEffects)
        {
            effect.Apply(user);
        }
        if (itemToUse.isConsumable)
        {
            RemoveItem(itemToUse);
        }
    }

    public void ClearInventory()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }

    public void ResetState()
    {
        ClearInventory();
        for (int i = 0; i < quickSlots.Length; i++)
        {
            quickSlots[i] = null;
        }
        OnQuickSlotsChanged?.Invoke();
    }

    public void AssignQuickSlot(int index, Objects item)
    {
        if (index < 0 || index >= quickSlots.Length) return;
        if (item != null)
        {
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == item && i != index)
                {
                    quickSlots[i] = null;
                }
            }
        }
        quickSlots[index] = item;
        OnQuickSlotsChanged?.Invoke();
    }

    // CORREÇÃO: retorno tipado (era 'object') para o JsonUtility conseguir
    // serializar o inventário dentro do GameData.
    public InventorySaveData GetSaveData()
    {
        InventorySaveData saveData = new InventorySaveData();
        foreach (var entry in items)
        {
            saveData.itemNames.Add(entry.item.name);
            saveData.itemQuantities.Add(entry.quantity);
        }
        foreach (var item in quickSlots)
        {
            if (item != null)
                saveData.quickSlotItemNames.Add(item.name);
            else
                saveData.quickSlotItemNames.Add(null);
        }
        return saveData;
    }

    // CORREÇÃO: parâmetro tipado (era 'object').
    public void LoadSaveData(InventorySaveData saveData)
    {
        if (saveData == null) return;
        items.Clear();
        for (int i = 0; i < saveData.itemNames.Count; i++)
        {
            Objects itemAsset = LoadItemAsset(saveData.itemNames[i]);
            if (itemAsset != null)
                items.Add(new InventoryItem(itemAsset, saveData.itemQuantities[i]));
            else
                Debug.LogWarning($"InventoryManager: não foi possível carregar o item '{saveData.itemNames[i]}'. " +
                                 "O ScriptableObject precisa estar em uma pasta 'Resources/Items/' (ou 'Resources/') com esse nome exato.");
        }
        for (int i = 0; i < saveData.quickSlotItemNames.Count && i < quickSlots.Length; i++)
        {
            if (!string.IsNullOrEmpty(saveData.quickSlotItemNames[i]))
            {
                quickSlots[i] = LoadItemAsset(saveData.quickSlotItemNames[i]);
            }
            else
            {
                quickSlots[i] = null;
            }
        }
        OnInventoryChanged?.Invoke();
        OnQuickSlotsChanged?.Invoke();
    }

    // Cache nome->asset construído uma vez (Resources.LoadAll percorre subpastas).
    private static Dictionary<string, Objects> _itemAssetCache;

    // CORREÇÃO: tenta 'Resources/Items/<nome>' e 'Resources/<nome>' (rápido) e,
    // como fallback robusto, varre TODAS as subpastas de Resources (ex.: Items/Chaves/),
    // indexando por nome de arquivo. Sem isso, chaves em subpastas sumiam ao carregar.
    private Objects LoadItemAsset(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        // 1) Tentativas diretas nos caminhos convencionais.
        Objects itemAsset = Resources.Load<Objects>("Items/" + itemName);
        if (itemAsset == null)
            itemAsset = Resources.Load<Objects>(itemName);
        if (itemAsset != null)
            return itemAsset;

        // 2) Fallback: indexa todos os Objects sob qualquer pasta Resources (recursivo).
        if (_itemAssetCache == null)
        {
            _itemAssetCache = new Dictionary<string, Objects>();
            foreach (Objects obj in Resources.LoadAll<Objects>(""))
            {
                if (obj != null && !_itemAssetCache.ContainsKey(obj.name))
                    _itemAssetCache[obj.name] = obj;
            }
        }

        _itemAssetCache.TryGetValue(itemName, out itemAsset);
        return itemAsset;
    }
}