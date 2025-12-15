using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeEffect : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Quanto tempo espera antes de começar a aparecer?")]
    public float startDelay = 0.5f;
    
    [Tooltip("Quanto tempo demora para ficar totalmente visível?")]
    public float fadeDuration = 2.0f;

    [Header("A Gracinha (Movimento)")]
    [Tooltip("Se marcado, o texto sobe um pouquinho enquanto aparece.")]
    public bool useSlideEffect = true;
    public float slideDistance = 50f; // Distância em pixels que ele sobe

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 finalPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Se for fazer o slide, guarda a posição final (onde ele está na cena agora)
        if (rectTransform != null)
        {
            finalPosition = rectTransform.anchoredPosition;
        }
    }

    void OnEnable()
    {
        // Garante que começa invisível toda vez que o objeto liga
        canvasGroup.alpha = 0f;
        
        if (useSlideEffect && rectTransform != null)
        {
            // Joga o texto um pouco pra baixo para ele subir depois
            rectTransform.anchoredPosition = finalPosition - new Vector2(0, slideDistance);
        }

        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        // 1. Espera o delay inicial (drama...)
        if (startDelay > 0)
            yield return new WaitForSecondsRealtime(startDelay);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // Usa unscaled para funcionar mesmo se o jogo pausar
            float percentage = timer / fadeDuration;

            // Curva suave (Ease Out) para ficar mais elegante que linear
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percentage);

            // Aplica o Fade
            canvasGroup.alpha = smoothPercent;

            // Aplica o Slide (A Gracinha)
            if (useSlideEffect && rectTransform != null)
            {
                // Lerp da posição "abaixo" para a posição "final"
                Vector2 startPos = finalPosition - new Vector2(0, slideDistance);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, finalPosition, smoothPercent);
            }

            yield return null;
        }

        // Garante valores finais exatos
        canvasGroup.alpha = 1f;
        if (useSlideEffect && rectTransform != null) rectTransform.anchoredPosition = finalPosition;
    }
}