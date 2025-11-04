using UnityEngine;
using UnityEngine.InputSystem;
public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação Única")]
    [Tooltip("ID único para o save. Ex: 'vilarejo_lenhador_01'")]
    public string npcID; //Save

    [Header("Estado Atual")]
    public NPCState currentState = NPCState.Corrompido;

    [Header("Lógica de Consequência (Design)")]
    [Tooltip("Arraste aqui o portão que este NPC abre se for purificado.")]
    public GameObject rotaParaAbrir; // 

    // (Aqui entra a lógica de IA/Combate)
    // ...
    // ...

    // O colega de IA/Combate chamaria isso quando a vida do NPC chegar a X
    // Dentro de CorruptedNPC.cs
    public void EntrarEmNocaute()
    {
        currentState = NPCState.Nocauteado;
        // 1. Para a IA de combate (ex: para de atacar)

        Debug.Log(npcID + " está nocauteado. O jogador pode decidir.");

        // 2. CHAMA A UI! 'this' significa (eu mesmo, este script)
        ChoiceUI.Instance.ShowChoice(this);
    }

    public void SerPurificado()
    {
        Debug.Log(npcID + " foi PURIFICADO.");
        currentState = NPCState.Purificado;

        if (rotaParaAbrir != null)
        {
            rotaParaAbrir.SetActive(false); // Ou 'true', dependendo de como for. Abre a rota.
        }

        WorldStateManager.Instance.SetNPCState(npcID, NPCState.Purificado);

        // 3. Destrói ou desativa o GameObject do NPC
        gameObject.SetActive(false);
    }

    // O Player (ou a UI) chamaria isso
    public void SerMorto()
    {
        Debug.Log(npcID + " foi MORTO.");
        currentState = NPCState.Morto;

        WorldStateManager.Instance.SetNPCState(npcID, NPCState.Morto);

        Destroy(gameObject);
    }

    // Função de teste 
    void Update()
{
    // --- A CLÁUSULA DE GUARDA ---
    // Se o jogo estiver pausado, NÃO execute a lógica de debug.
    if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
    {
        return; // Sai da função imediatamente
    }
    // -----------------------------

    // Verifica se a tecla 'P' foi pressionada *neste frame*
    if (Keyboard.current.pKey.wasPressedThisFrame)
    {
        SerPurificado();
    }

    // Verifica se a tecla 'K' foi pressionada *neste frame*
    if (Keyboard.current.kKey.wasPressedThisFrame)
    {
        SerMorto(); // <-- A linha que estava sendo chamada
    }

    // Verifica se a tecla 'N' foi pressionada *neste frame* (para teste de Nocaute)
    if (Keyboard.current.nKey.wasPressedThisFrame)
    {
        EntrarEmNocaute();
    }
}
    void Start()
    {
        NPCState estadoSalvo;

        if (WorldStateManager.Instance.npcWorldStates.TryGetValue(this.npcID, out estadoSalvo))
        {
            // Se o estado salvo for Purificado ou Morto, eu devo sumir.
            if (estadoSalvo == NPCState.Purificado || estadoSalvo == NPCState.Morto)
            {
                // ATUALIZA O MUNDO REAL COM O DADO SALVO
                if (rotaParaAbrir != null && estadoSalvo == NPCState.Purificado)
                {
                    rotaParaAbrir.SetActive(false); // Abre a rota
                }
                
                gameObject.SetActive(false); // Some com o NPC
            }
       
        }
      
    }

}