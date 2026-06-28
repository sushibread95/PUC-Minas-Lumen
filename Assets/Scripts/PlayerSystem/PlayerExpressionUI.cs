using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Expressão da personagem na HUD. Escuta os eventos do HealthSystem e troca o
// sprite de uma Image conforme o estado da vida:
//   - normal: estado de descanso;
//   - hurt: aparece por um instante ao tomar dano, depois volta ao descanso;
//   - lowHealth (opcional): substitui o "normal" quando a vida está baixa;
//   - dead (opcional): ao morrer.
//
// Todos os sprites (menos a Image) são opcionais — o que ficar vazio é ignorado,
// então você pode começar só com normal + hurt e adicionar o resto depois.
public class PlayerExpressionUI : MonoBehaviour
{
    [Header("Onde o sprite aparece")]
    [Tooltip("A Image da HUD onde a expressão é desenhada.")]
    public Image expressionImage;

    [Header("Expressões (sprites)")]
    [Tooltip("Rosto de descanso (estado padrão).")]
    public Sprite normalSprite;
    [Tooltip("Rosto ao tomar dano (mostrado por alguns instantes).")]
    public Sprite hurtSprite;
    [Tooltip("Opcional: rosto ao atacar (mostrado por alguns instantes).")]
    public Sprite attackingSprite;
    [Tooltip("Opcional: rosto quando a vida está baixa (substitui o normal).")]
    public Sprite lowHealthSprite;
    [Tooltip("Opcional: rosto ao morrer.")]
    public Sprite deadSprite;

    [Header("Configuração")]
    [Tooltip("Fração de vida abaixo da qual usa o rosto de vida baixa (0.3 = 30%).")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
    [Tooltip("Quanto tempo o rosto de dano fica na tela (segundos).")]
    public float hurtDuration = 0.6f;
    [Tooltip("Quanto tempo o rosto de ataque fica na tela (segundos).")]
    public float attackDuration = 0.4f;

    private Coroutine tempRoutine;
    private bool isDead;
    private float healthPercent = 1f;

    private void OnEnable()
    {
        HealthSystem.OnPlayerDamaged += HandleDamaged;
        HealthSystem.OnPlayerHealthChanged += HandleHealthChanged;
        HealthSystem.OnPlayerDied += HandleDied;
        PlayerControllerSystem.OnPlayerAttacked += HandleAttacked;
        ApplyRestingExpression();
    }

    private void OnDisable()
    {
        HealthSystem.OnPlayerDamaged -= HandleDamaged;
        HealthSystem.OnPlayerHealthChanged -= HandleHealthChanged;
        HealthSystem.OnPlayerDied -= HandleDied;
        PlayerControllerSystem.OnPlayerAttacked -= HandleAttacked;
    }

    // Tomou dano: mostra o rosto machucado por um tempo, depois volta ao descanso.
    private void HandleDamaged()
    {
        if (isDead || hurtSprite == null) return;
        PlayTemporary(hurtSprite, hurtDuration);
    }

    // Atacou: mostra o rosto de ataque por um tempo, depois volta ao descanso.
    private void HandleAttacked()
    {
        if (isDead || attackingSprite == null) return;
        PlayTemporary(attackingSprite, attackDuration);
    }

    // Mostra um sprite temporário (dano ou ataque); o mais recente substitui o anterior.
    private void PlayTemporary(Sprite sprite, float duration)
    {
        if (tempRoutine != null) StopCoroutine(tempRoutine);
        tempRoutine = StartCoroutine(TemporaryRoutine(sprite, duration));
    }

    private IEnumerator TemporaryRoutine(Sprite sprite, float duration)
    {
        SetSprite(sprite);
        yield return new WaitForSecondsRealtime(duration);
        tempRoutine = null;
        ApplyRestingExpression();
    }

    // Vida mudou (dano, cura, ou (re)spawn): guarda a fração e atualiza o descanso.
    private void HandleHealthChanged(float current, float max)
    {
        healthPercent = max > 0f ? current / max : 1f;

        // Vida positiva = personagem viva (reseta caso tenha vindo de uma morte).
        if (current > 0f) isDead = false;

        // Só troca o "descanso" se não estiver no meio de um flash temporário (dano/ataque).
        if (tempRoutine == null) ApplyRestingExpression();
    }

    private void HandleDied()
    {
        isDead = true;
        if (tempRoutine != null) { StopCoroutine(tempRoutine); tempRoutine = null; }
        if (deadSprite != null) SetSprite(deadSprite);
    }

    // Define a expressão de descanso: morta > vida baixa > normal, conforme disponível.
    private void ApplyRestingExpression()
    {
        if (isDead)
        {
            if (deadSprite != null) SetSprite(deadSprite);
            return;
        }

        if (lowHealthSprite != null && healthPercent <= lowHealthThreshold)
            SetSprite(lowHealthSprite);
        else
            SetSprite(normalSprite);
    }

    private void SetSprite(Sprite sprite)
    {
        if (expressionImage != null && sprite != null)
            expressionImage.sprite = sprite;
    }
}
