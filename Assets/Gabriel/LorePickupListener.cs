using UnityEngine;
using UnityEngine.Events;

public class LorePickupListener : MonoBehaviour
{
    [Header("Configuração (Arraste o Item Aqui)")]
    [Tooltip("Arraste o ScriptableObject do item que dispara a lore (Ex: Bilhete_start)")]
    public Objects targetItemObject; // --- MUDANÇA: Usa o seu script Objects ---

    [Tooltip("Se marcado, o script se destrói após tocar (toca uma vez só)")]
    public bool playOnce = true;

    [Header("O que acontece?")]
    public UnityEvent onPickup;

    void OnEnable()
    {
        GameEvents.OnItemObtained += HandleItemObtained;
    }

    void OnDisable()
    {
        GameEvents.OnItemObtained -= HandleItemObtained;
    }

    private void HandleItemObtained(string itemID, int quantity)
    {
        // SEGURANÇA: Se esqueceu de arrastar o item, avisa e ignora
        if (targetItemObject == null)
        {
            Debug.LogWarning("[Lore] Nenhum item configurado no LorePickupListener!");
            return;
        }

        // --- LÓGICA CORRIGIDA ---
        // O InventoryManager envia o 'itemID' que é igual ao item.name (Nome do Arquivo na Project Window).
        // Então comparamos com o .name do objeto arrastado, e não com o campo interno .objectName.
        if (itemID == targetItemObject.name)
        {
            Debug.Log($"[Lore] Item '{itemID}' reconhecido! Disparando evento...");
            
            onPickup?.Invoke();

            if (playOnce)
            {
                GameEvents.OnItemObtained -= HandleItemObtained;
                Destroy(this); 
            }
        }
        else
        {
            // Debug opcional para ver o que está passando
            // Debug.Log($"[Lore] Ignorando item '{itemID}'. Esperando por '{targetItemObject.name}'");
        }
    }
}