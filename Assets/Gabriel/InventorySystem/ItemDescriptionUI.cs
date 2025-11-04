using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDescriptionUI : MonoBehaviour
{
    public static ItemDescriptionUI Instance;

    [Header("UI References")]
    public GameObject panel;
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI descriptionText;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowDescription(Objects item)
    {
        if (item == null) return;

        iconImage.sprite = item.icon;
        itemNameText.text = item.objectName;
        descriptionText.text = item.description;
        panel.SetActive(true);
    }

    public void HideDescription()
    {
        panel.SetActive(false);
    }
}
