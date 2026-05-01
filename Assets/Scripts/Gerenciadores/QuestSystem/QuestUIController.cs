using UnityEngine;
using System.Collections.Generic;

public class QuestUIController : MonoBehaviour
{
    public static QuestUIController Instance { get; private set; }

    [Header("Configuração")]
    public GameObject questPanel;      // O painel inteiro desta aba
    public Transform questListContent; // O objeto "Content" dentro do ScrollView
    public GameObject questSlotPrefab; // O prefab que tem o script QuestSlotUI

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Chamado pelo CharacterMenuWindow
    public void OnQuestTabOpened()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        if (QuestManager.Instance == null) return;

        // 1. Apaga a lista antiga
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        // 2. Busca as quests ativas no Manager
        List<QuestSaveData> activeQuests = QuestManager.Instance.GetActiveQuestsSaveData();

        // 3. Cria um slot para cada quest
        foreach (QuestSaveData qData in activeQuests)
        {
            QuestDefinition def = QuestManager.Instance.GetQuestDefinition(qData.questID);
            
            if (def != null)
            {
                GameObject newSlot = Instantiate(questSlotPrefab, questListContent);
                QuestSlotUI ui = newSlot.GetComponent<QuestSlotUI>();
                if (ui) ui.Setup(qData, def);
            }
        }
        
        // (Opcional) Aqui você poderia buscar as quests completas também e listar no final
    }
}