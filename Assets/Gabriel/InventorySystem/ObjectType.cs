using UnityEngine;

public class ObjectType : MonoBehaviour
{   
    public Objects TypeObjec; //

    [Header("Identificação Única (Save)")]
    [Tooltip("ID único para este item NO MUNDO. Ex: 'Potion_By_The_Well'. DEIXE EM BRANCO se for um drop de inimigo que NÃO deve ser salvo.")]
    public string itemInstanceID;

    void Start()
    {
        if (!string.IsNullOrEmpty(itemInstanceID) && 
            WorldStateManager.Instance != null && 
            WorldStateManager.Instance.IsItemCollected(itemInstanceID))
        {
            Debug.Log($"Item {itemInstanceID} já foi coletado. Destruindo.");
            Destroy(gameObject);
        }
    }
}