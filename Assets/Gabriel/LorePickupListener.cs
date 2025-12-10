using UnityEngine;
using UnityEngine.Events;

public class LorePickupListener : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("O ID do item que vai disparar esse diálogo (Ex: chave_moinho)")]
    public string targetItemID;

    [Tooltip("Se marcado, o script se destrói após tocar (toca uma vez só)")]
    public bool playOnce = true;

    [Header("O que acontece?")]
    [Tooltip("Arraste aqui o seu DialogueTrigger ou função de Lore")]
    public UnityEvent onPickup;

    void OnEnable()
    {
        // Se inscreve para saber quando itens são pegos
        GameEvents.OnItemObtained += HandleItemObtained;
    }

    void OnDisable()
    {
        GameEvents.OnItemObtained -= HandleItemObtained;
    }

    private void HandleItemObtained(string itemID, int quantity)
    {
        // Verifica se o item pego é o que estamos esperando
        if (itemID == targetItemID)
        {
            Debug.Log($"[Lore] Item {itemID} coletado. Tocando lore...");
            
            // Dispara o evento (Toca o diálogo)
            onPickup?.Invoke();

            if (playOnce)
            {
                // Se remove para não tocar de novo
                GameEvents.OnItemObtained -= HandleItemObtained;
                Destroy(this); 
            }
        }
    }
}