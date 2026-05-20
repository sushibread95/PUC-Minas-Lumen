using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EnemyIdentity : MonoBehaviour
{
    #region Inspector - Identity

    [Header("IDENTIDADE DO INIMIGO")]
    [Tooltip("ID único desta instância na cena. Use para save/estado individual. Ex: vila_01_cultista_003")]
    [SerializeField] private string uniqueID;

    [Tooltip("ID usado pelas quests de matar inimigos. Vários inimigos podem compartilhar este ID. Ex: cultista_corrompido")]
    [SerializeField] private string questKillID;

    [Tooltip("Tipo do inimigo para organização/debug. Ex: melee, ranged, grabber")]
    [SerializeField] private string enemyTypeID = "enemy";

    [Tooltip("Grupo/encontro opcional. Ex: arena_vila_01")]
    [SerializeField] private string encounterID;

    [Tooltip("Gera um ID único estável por cena/nome/posição se Unique ID estiver vazio.")]
    [SerializeField] private bool autoGenerateUniqueID = true;

    #endregion

    #region Runtime

    private bool deathEventSent;

    #endregion

    #region Public API

    public string UniqueID => uniqueID;
    public string QuestKillID => !string.IsNullOrWhiteSpace(questKillID) ? questKillID : enemyTypeID;
    public string EnemyTypeID => enemyTypeID;
    public string EncounterID => encounterID;
    public bool DeathEventSent => deathEventSent;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        EnsureUniqueID();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(enemyTypeID))
            enemyTypeID = "enemy";
    }

    #endregion

    #region Setup

    // Garante que a instância tenha um ID individual sem misturar com o ID usado pelas quests.
    public void EnsureUniqueID()
    {
        if (!autoGenerateUniqueID || !string.IsNullOrWhiteSpace(uniqueID))
            return;

        Scene scene = gameObject.scene;
        string sceneName = scene.IsValid() ? scene.name : "scene";
        Vector3 p = transform.position;
        uniqueID = $"{sceneName}_{gameObject.name}_{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}_{Mathf.RoundToInt(p.z * 10f)}";
    }

    #endregion

    #region Events

    // Dispara o evento de morte uma única vez, usando o ID correto para quests.
    public void NotifyDeathForQuest()
    {
        if (deathEventSent)
            return;

        deathEventSent = true;

        string id = QuestKillID;
        if (string.IsNullOrWhiteSpace(id))
            return;

        GameEvents.TriggerEnemyDeath(id);
    }

    #endregion
}
