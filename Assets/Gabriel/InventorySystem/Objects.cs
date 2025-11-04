using UnityEngine;

[CreateAssetMenu(fileName = "NewObject", menuName = "Inventory Objects/Create New")]
public class Objects : ScriptableObject
{
    public string objectName;
    [TextArea(2, 5)]
    public string description; // ← novo campo de descrição
    public Sprite icon;
    public int maxStack = 99;
}
