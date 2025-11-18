using UnityEngine;
using TMPro;

// Este script vai no seu GameObject 'TextMeshPro - Text'
[RequireComponent(typeof(TextMeshProUGUI))]
public class FeedbackUIRegistrar : MonoBehaviour
{
    void Start()
    {
        // Pega o componente de texto deste objeto
        TextMeshProUGUI myTextComponent = GetComponent<TextMeshProUGUI>();

        // Tenta se registrar no Manager (que está na cena Boot)
        if (UIFeedbackManager.Instance != null)
        {
            UIFeedbackManager.Instance.RegisterFeedbackText(myTextComponent);
        }
        else
        {
            Debug.LogError("FeedbackUIRegistrar: Não conseguiu encontrar o UIFeedbackManager.Instance!");
        }
    }
}