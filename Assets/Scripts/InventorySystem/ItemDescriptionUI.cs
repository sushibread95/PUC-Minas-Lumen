using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDescriptionUI : MonoBehaviour
{
    public static ItemDescriptionUI Instance { get; private set; } // Mudado para 'private set'

    [Header("UI References")]
    public GameObject panel;
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI descriptionText;

    void Awake()
    {
        // --- INÍCIO DA CORREÇÃO ---
        // Singleton aprimorado (impede duplicatas)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // O script (this.gameObject) continua ATIVO,
        // mas o painel visual (panel) é escondido.
        if (panel != null)
        {
            panel.SetActive(false);
        }
        // --- FIM DA CORREÇÃO ---
    }

    public void ShowDescription(Objects item)
    {
        if (item == null)
        {
            HideDescription(); // Esconde se o item for nulo
            return;
        }

        iconImage.sprite = item.icon;
        iconImage.enabled = (item.icon != null); // Só mostra se tiver ícone
        itemNameText.text = item.objectName;
        descriptionText.text = item.description;

        if (panel != null)
            panel.SetActive(true);
    }

    public void HideDescription()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}