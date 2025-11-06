using UnityEngine;

public class ContextualPromptUI : MonoBehaviour
{

    void Awake()
    {
        gameObject.SetActive(false); 
    }

    public void Show()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            return;
            
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}