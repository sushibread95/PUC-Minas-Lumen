using UnityEngine;
using UnityEngine.UI;

public class LockOnReticle : MonoBehaviour
{
    [Header("Dynamic References")]
    public Camera cam;
    public LockOnSystem lockOn;
    
    [Header("Settings")]
    public Image img;
    public Vector2 screenOffset = new Vector2(0, 0); 

    void Awake()
    {
        if (!img) img = GetComponent<Image>();
        // Começa desligado para não mostrar uma mira parada no meio do nada
        if (img) img.enabled = false;
    }

    void LateUpdate()
    {
        // 1. TENTA ACHAR A CÂMERA (Se perdeu)
        if (cam == null)
        {
            cam = Camera.main; // Procura quem tem a tag "MainCamera"
            if (cam == null) return; // Ainda não nasceu? Aborta.
        }

        // 2. TENTA ACHAR O PLAYER (Se perdeu)
        if (lockOn == null)
        {
            // Procura o script na cena (lento, mas só roda uma vez até achar)
            lockOn = FindAnyObjectByType<LockOnSystem>(); 
            if (lockOn == null) return; // Player não nasceu? Aborta.
        }

        // 3. SEGURANÇA VISUAL
        if (!img) return;

        // Se o sistema não estiver travado em ninguém, esconde a mira
        if (!lockOn.IsLockedOn)
        {
            if (img.enabled) img.enabled = false;
            return;
        }

        // Se perdeu o alvo no meio do caminho, esconde
        Transform aim = lockOn.CurrentAimPoint;
        if (!aim)
        {
            if (img.enabled) img.enabled = false;
            return;
        }

        // 4. CÁLCULO DE POSIÇÃO
        Vector3 sp = cam.WorldToScreenPoint(aim.position);
        
        // Verifica se o alvo está na frente da câmera (Z > 0)
        bool onScreen = sp.z > 0f; 
        
        // Verifica se está dentro da resolução da tela (opcional, mas bom pra evitar glitche na borda)
        if (onScreen)
        {
            if (!img.enabled) img.enabled = true;
            
            // Lógica para Canvas Overlay (Padrão)
            // Se o Pivot do RectTransform for 0.5, 0.5 (Centro), precisamos subtrair metade da tela
            // SE o Canvas for Screen Space - Camera, a lógica muda um pouco, mas tente esta primeiro:
            img.rectTransform.anchoredPosition = (Vector2)sp - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + screenOffset;
        }
        else
        {
            if (img.enabled) img.enabled = false;
        }
    }
}