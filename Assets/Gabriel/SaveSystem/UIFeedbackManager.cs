using UnityEngine;
using TMPro; 
using System.Collections;

public class UIFeedbackManager : MonoBehaviour
{
    public TextMeshProUGUI saveFeedbackText;

    void OnEnable()
    {
        SaveManager.OnGameSaved += ShowSaveNotification;
    }

    void OnDisable()
    {
        SaveManager.OnGameSaved -= ShowSaveNotification;
    }

    void ShowSaveNotification()
    {
        if (saveFeedbackText != null)
        {
            StartCoroutine(ShowFeedbackRoutine());
        }
    }

    private IEnumerator ShowFeedbackRoutine()
    {
        saveFeedbackText.gameObject.SetActive(true);
        saveFeedbackText.text = "Salvando..."; 

        yield return new WaitForSeconds(2f); 

        saveFeedbackText.text = "";
        saveFeedbackText.gameObject.SetActive(false);
    }
}