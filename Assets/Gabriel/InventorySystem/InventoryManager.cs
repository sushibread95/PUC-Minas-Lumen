using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

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

    public List<InventoryItem> items = new List<InventoryItem>();

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
        foreach (var entry in items)
        {
            if (entry.item == newItem)
            {
                if (entry.quantity < newItem.maxStack)
                {
                    entry.quantity++;
                    return;
                }
                else
                {
                    return; 
                }
            }
        }
        items.Add(new InventoryItem(newItem, 1));
    }

    public void RemoveItem(Objects itemToRemove)
    {
        InventoryItem entry = items.Find(x => x.item == itemToRemove);
        if (entry != null)
        {
            entry.quantity--;
            if (entry.quantity <= 0)
                items.Remove(entry);
        }
    }

    public void ClearInventory()
    {
        items.Clear();
    }

    public void ResetState()
    {
        items.Clear();
        Debug.Log("InventoryManager RESETADO para Novo Jogo.");
    }

    public List<InventoryItemSaveData> GetSaveData()
    {
        List<InventoryItemSaveData> dataToSave = new List<InventoryItemSaveData>();
        foreach (var entry in items)
        {
            dataToSave.Add(new InventoryItemSaveData
            {
                itemID = entry.item.objectName,
                quantity = entry.quantity
            });
        }
        return dataToSave;
    }

    // 4. Carrega os dados do save
    public void LoadSaveData(List<InventoryItemSaveData> dataToLoad)
    {
        items.Clear();
        if (dataToLoad == null) return;

        foreach (var entry in dataToLoad)
        {
            
            
            Objects itemAsset = Resources.Load<Objects>("Items/" + entry.itemID);
            if (itemAsset != null)
            {
                items.Add(new InventoryItem(itemAsset, entry.quantity));
            }
            else
            {
                Debug.LogWarning($"Não foi possível encontrar o item com ID: {entry.itemID} no Resources/Items");
            }
        }
        Debug.Log($"InventoryManager carregou {items.Count} itens.");
    }
}