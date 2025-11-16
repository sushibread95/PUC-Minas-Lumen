using UnityEngine;

public class ObjectType : MonoBehaviour
{
    [Header("Para o Inventário")]
    public Objects TypeObjec; // O ScriptableObject com os dados do item

    [Header("Para o Save/Load")]
    public string id; // ID único deste item na cena

    // !! LÓGICA MOVIDA PARA CÁ !!
    // Este item vai se checar sozinho ao iniciar a cena
    void Start()
    {
        // Se o WorldState existe E se este item tem um ID
        if (WorldStateManager.Instance != null && !string.IsNullOrEmpty(id))
        {
            // Se o WorldState disser que este ID já foi coletado...
            if (WorldStateManager.Instance.IsItemCollected(id))
            {
                // ...então o item se desativa.
                gameObject.SetActive(false);
            }
        }
    }

    // Opcional: Gerar ID automático
    void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            // Debug.Log($"Novo ID gerado para {gameObject.name}");
        }
    }
}