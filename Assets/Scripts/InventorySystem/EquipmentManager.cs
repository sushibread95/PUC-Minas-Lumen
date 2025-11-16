using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    // Evento para a UI de equipamento saber que mudou
    public static event System.Action OnEquipmentChanged;

    // Exemplo de slots de equipamento. Você pode usar um Enum
    // ou classes mais complexas, mas um Dicionário é um bom começo.
    public Dictionary<EquipmentSlotType, Objects> equippedItems;

    // Defina os tipos de slot que seu jogo terá
    public enum EquipmentSlotType { Capacete, Peitoral, Amuleto, Arma }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            equippedItems = new Dictionary<EquipmentSlotType, Objects>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EquipItem(Objects item)
    {
        // Lógica para descobrir qual slot este item usa
        // (Por enquanto, vamos simplificar)

        // if (item.objectName.Contains("Capacete"))
        // {
        //     equippedItems[EquipmentSlotType.Capacete] = item;
        // }

        Debug.Log($"Equipando {item.objectName}. (Lógica de slot necessária)");

        // TODO: Aplicar stats de equipamento (item.equipEffects)

        OnEquipmentChanged?.Invoke();
    }
}