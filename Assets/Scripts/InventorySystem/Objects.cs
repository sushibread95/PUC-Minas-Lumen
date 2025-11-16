using UnityEngine;
using System.Collections.Generic; // Necessário para Listas

[CreateAssetMenu(fileName = "NewObject", menuName = "Inventory Objects/Create New")]
public class Objects : ScriptableObject
{
    [Header("Info")]
    public string objectName;
    [TextArea(2, 5)]
    public string description;
    public Sprite icon;
    public int maxStack = 99;

    [Header("Item Type")]
    public bool isConsumable = true; // Para o "Usar" (Feature 3)
    public bool isEquippable = false; // Para o "Equipar" (Feature 2)

    [Header("Effects")]
    // Efeitos que acontecem ao "Usar" o item
    public List<ItemEffect> useEffects;

    // Efeitos que acontecem ao "Equipar" (ex: +10 de Defesa)
    // (Ainda não vamos usar, mas é bom ter)
    // public List<ItemEffect> equipEffects;
}