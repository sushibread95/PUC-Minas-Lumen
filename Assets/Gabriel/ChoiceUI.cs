using UnityEngine;
using UnityEngine.UI;

public class ChoiceUI : MonoBehaviour
{
    public Button purifyButton;
    public Button killButton;

    // "Memória" de curto prazo para saber QUEM estamos julgando
    private CorruptedNPC currentNPC; 

    private static ChoiceUI _instance;
    public static ChoiceUI Instance

    {
        get
        {
            if (_instance == null)
            {
                // Procura o objeto na cena, *incluindo objetos inativos* (o 'true')
                _instance = Object.FindFirstObjectByType<ChoiceUI>(FindObjectsInactive.Include);              
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        // O Awake() ainda é útil para configurar os botões.
        // Mas ele NÃO vai mais ser responsável pelo Singleton ou por se esconder.
        purifyButton.onClick.AddListener(OnPurify);
        killButton.onClick.AddListener(OnKill);
    }

    public void ShowChoice(CorruptedNPC npc)
{
    if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
    {
        return;
    }

    this.currentNPC = npc;

    gameObject.SetActive(true);

    Time.timeScale = 0f;
}

    private void OnPurify()
    {
        if (this.currentNPC != null)
        {
            this.currentNPC.SerPurificado();
        }
        HidePanel();
    }

    private void OnKill()
    {
        if (this.currentNPC != null)
        {
            this.currentNPC.SerMorto();
        }
        HidePanel();
    }

private void HidePanel()
{
    Time.timeScale = 1f;

    this.currentNPC = null;
    gameObject.SetActive(false);
}
}