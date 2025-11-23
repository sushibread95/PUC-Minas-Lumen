using UnityEngine;

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação")]
    public string npcID;

    [Header("Recompensas")]
    [Tooltip("Quanto de XP esse inimigo dá ao ser resolvido?")]
    public float xpReward = 50f; // Valor base

    [Header("Estado")]
    public NPCState currentState = NPCState.Corrompido;
    public GameObject rotaParaAbrir;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(npcID)) npcID = System.Guid.NewGuid().ToString();
    }

    public void EntrarEmNocaute()
    {
        if (currentState != NPCState.Corrompido) return;
        currentState = NPCState.Nocauteado;
        // Tocar animação de nocaute aqui
    }

    public void SerPurificado()
    {
        currentState = NPCState.Purificado;

        // --- ADIÇÃO: Dá XP Verde + XP Base ---
        if (LevelingSystem.Instance != null)
        {
            LevelingSystem.Instance.AddPurificationXP(xpReward);
        }

        if (rotaParaAbrir != null) rotaParaAbrir.SetActive(false);
        if (WorldStateManager.Instance != null) WorldStateManager.Instance.SetNPCState(npcID, NPCState.Purificado);

        gameObject.SetActive(false);
    }

    public void SerMorto()
    {
        currentState = NPCState.Morto;

        // --- ADIÇÃO: Dá XP Vermelho + XP Base ---
        if (LevelingSystem.Instance != null)
        {
            LevelingSystem.Instance.AddCombatXP(xpReward);
        }

        if (WorldStateManager.Instance != null) WorldStateManager.Instance.SetNPCState(npcID, NPCState.Morto);

        Destroy(gameObject);
    }

    // ... (Start permanece igual para carregar estado) ...
    void Start()
    {
        if (WorldStateManager.Instance == null) return;
        if (WorldStateManager.Instance.npcWorldStates.TryGetValue(this.npcID, out NPCState estadoSalvo))
        {
            if (estadoSalvo == NPCState.Purificado || estadoSalvo == NPCState.Morto)
            {
                if (rotaParaAbrir != null && estadoSalvo == NPCState.Purificado) rotaParaAbrir.SetActive(false);
                gameObject.SetActive(false);
            }
        }
    }
}