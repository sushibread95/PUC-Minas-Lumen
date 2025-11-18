using UnityEngine;

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação Única")]
    [Tooltip("ID único para o save. Ex: 'vilarejo_lenhador_01'")]
    public string npcID; //Save

    [Header("Estado Atual")]
    public NPCState currentState = NPCState.Corrompido;

    [Header("Lógica de Consequência (Design)")]
    [Tooltip("Arraste aqui o portão que este NPC abre se for purificado.")]
    public GameObject rotaParaAbrir;

    [Header("Configuração de Nocaute")]
    public float interactionRadius = 3f;

    // --- AVISO DE MODIFICAÇÃO (ADIÇÃO) ---
    void OnValidate()
    {
        if (string.IsNullOrEmpty(npcID))
        {
            npcID = System.Guid.NewGuid().ToString();
        }
    }

    
    public void EntrarEmNocaute()
    {
        if (currentState != NPCState.Corrompido) return;

        currentState = NPCState.Nocauteado;
        Debug.Log(npcID + " foi nocauteado. O jogador pode decidir.");
    }

    public void SerPurificado()
    {
        Debug.Log(npcID + " foi PURIFICADO.");
        currentState = NPCState.Purificado;
        if (rotaParaAbrir != null) rotaParaAbrir.SetActive(false);
        
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.SetNPCState(npcID, NPCState.Purificado);
        
        gameObject.SetActive(false);
    }

    public void SerMorto()
    {
        Debug.Log(npcID + " foi MORTO.");
        currentState = NPCState.Morto;
        
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.SetNPCState(npcID, NPCState.Morto);
        
        Destroy(gameObject);
    }

    void Start()
    {
        if (WorldStateManager.Instance == null) return;

        NPCState estadoSalvo;
        if (WorldStateManager.Instance.npcWorldStates.TryGetValue(this.npcID, out estadoSalvo))
        {
            if (estadoSalvo == NPCState.Purificado || estadoSalvo == NPCState.Morto)
            {
                if (rotaParaAbrir != null && estadoSalvo == NPCState.Purificado)
                {
                    rotaParaAbrir.SetActive(false);
                }
                gameObject.SetActive(false); // Some com o NPC
            }
        }
    }
}