// Nome do arquivo: ObjectType.cs
// CÓDIGO COMPLETO (MODIFICADO PARA IMPLEMENTAR IINTERACTABLE)

using UnityEngine;

// --- AVISO DE MODIFICAÇÃO ---
// Adicionamos ', IInteractable' para que este script
// possa ser "visto" pelo novo InteractionManager.
// --- FIM DO AVISO ---
public class ObjectType : MonoBehaviour, IInteractable
{
    [Header("Para o Inventário")]
    public Objects TypeObjec; // O ScriptableObject com os dados do item

    [Header("Para o Save/Load")]
    public string id; // ID único deste item na cena

    // Lógica original do seu colega (perfeita)
    void Start()
    {
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(id))
        {
            if (WorldStateManager.Instance.IsItemCollected(id))
            {
                gameObject.SetActive(false);
            }
        }
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }

    // --- INÍCIO DA ADIÇÃO (CONTRATO) ---

    // 1. Função obrigatória do IInteractable
    // Retorna o texto para o prompt da UI
    public string GetInteractText()
    {
        if (TypeObjec != null)
            return $"Pegar {TypeObjec.objectName}";
        else
            return "Pegar Item";
    }

    // 2. Função obrigatória do IInteractable
    // Esta é a lógica que MOIVEMOS do ItemPickupSystem
    public void Interact()
    {
        Debug.Log($"INTERAGINDO COM: {gameObject.name}");

        // 1. Adiciona ao inventário
        if (InventoryManager.Instance != null && TypeObjec != null)
        {
            InventoryManager.Instance.AddItem(TypeObjec);
        }
        else
        {
            Debug.LogError("Inventário ou 'TypeObjec' nulo!");
            return;
        }

        // 2. Registra no Save
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(id))
        {
            WorldStateManager.Instance.RegisterCollectedItem(id);
        }
        else
        {
            Debug.LogWarning($"Item {name} não tem ID ou WorldStateManager não foi encontrado. Não será salvo.");
        }

        // 3. Destrói o objeto
        Destroy(gameObject);
    }
    // --- FIM DA ADIÇÃO ---
}