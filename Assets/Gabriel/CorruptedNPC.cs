// Nome do arquivo: CorruptedNPC.cs
// CÓDIGO COMPLETO E LIMPO (MODIFICADO - SEM LÓGICA DE INPUT)

using UnityEngine;
// using UnityEngine.InputSystem; // Não é mais necessário aqui

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação Única")]
    public string npcID; //Save

    [Header("Estado Atual")]
    public NPCState currentState = NPCState.Corrompido;

    [Header("Lógica de Consequência (Design)")]
    public GameObject rotaParaAbrir;

    [Header("Configuração de Nocaute")]
    public float interactionRadius = 3f;

    // --- MODIFICAÇÃO ---
    // Não precisamos mais do 'Awake()' para desligar o FinishableNPC
    // --- FIM DA MODIFICAÇÃO ---
    
    // O NPCInteraction.cs chama esta função
    public void EntrarEmNocaute()
    {
        // Se já foi nocauteado, não faz nada
        if (currentState != NPCState.Corrompido) return;

        currentState = NPCState.Nocauteado;
        // 1. Para a IA de combate (ex: para de atacar)
        // ...

        Debug.Log(npcID + " foi nocauteado. O jogador pode decidir.");
        
        // 2. --- MODIFICAÇÃO ---
        // Ele NÃO chama mais a UI.
        // Ele apenas MUDA O ESTADO. O NPCInteraction.cs vai ver isso.
        // --- FIM DA MODIFICAÇÃO ---
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

    // --- MODIFICAÇÃO ---
    // A função Update() inteira foi removida.
    // O NPCInteraction.cs agora cuida de toda a lógica de input.
    // --- FIM DA MODIFICAÇÃO ---
    
    void Start()
    {
        // A lógica de Load continua perfeita
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
                gameObject.SetActive(false);
            }
        }
    }
}