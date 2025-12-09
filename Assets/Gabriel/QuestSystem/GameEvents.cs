using System;
using UnityEngine;

public static class GameEvents
{
    // Disparado quando um inimigo morre (ID do inimigo)
    public static Action<string> OnEnemyDeath;

    // Disparado quando um item é coletado (ID/Name do item, Quantidade)
    public static Action<string, int> OnItemObtained;

    // Disparado quando uma quest avança ou conclui (para atualizar UI)
    public static Action OnQuestProgressChanged;

    // Métodos auxiliares para disparar com segurança
    public static void TriggerEnemyDeath(string enemyID) => OnEnemyDeath?.Invoke(enemyID);
    public static void TriggerItemObtained(string itemID, int quantity) => OnItemObtained?.Invoke(itemID, quantity);
    public static void TriggerQuestProgressChanged() => OnQuestProgressChanged?.Invoke();
}