using UnityEngine;
using UnityEngine.UI;

// Este script vai em um objeto 'Image' separado no seu Canvas
[RequireComponent(typeof(Image))]
public class DragDropIcon : MonoBehaviour
{
    public static DragDropIcon Instance;

    private Image iconImage;

    void Awake()
    {
        Instance = this;
        iconImage = GetComponent<Image>();
        iconImage.raycastTarget = false; // Impede que o ícone bloqueie o mouse
        HideIcon();
    }

    void Update()
    {
        // Faz o ícone seguir o mouse
        if (iconImage.enabled)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void ShowIcon(Sprite sprite)
    {
        if (sprite == null) return;
        iconImage.sprite = sprite;
        iconImage.enabled = true;
    }

    public void HideIcon()
    {
        iconImage.enabled = false;
        iconImage.sprite = null;
    }
}