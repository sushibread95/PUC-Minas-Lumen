using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyGrabber : MonoBehaviour
{
    [Header("Configuração de Snap")]
    public Transform grabSnapPoint; 
    
    [Header("Dano")]
    public float damagePerSecond = 5f;
    public float maxGrabDuration = 5.0f;
    
    [Header("Timing (O Segredo!)")]
    [Tooltip("Quanto tempo esperar depois de iniciar a animação para 'Ligar' o agarrão")]
    public float grabWindupTime = 0.4f; // Ajuste isso! (Tempo de levantar os braços)
    [Tooltip("Quanto tempo a 'janela' de agarrar fica aberta")]
    public float grabActiveDuration = 0.5f; // Ajuste isso! (Tempo do abraço)

    [Header("Animação")]
    public string grabSuccessTrigger = "GrabSuccess"; 
    public string grabBrokenTrigger = "GrabBroken";   
    public string grabFinishTrigger = "GrabFinish";   

    // ESTADO INTERNO
    private bool isGrabbing = false;      // Já pegou o player?
    private bool canCatchPlayer = false;  // A janela de captura está aberta agora?
    
    private PlayerControllerSystem capturedPlayer;
    private EnemyHealth myHealth;
    private Animator myAnimator;
    private Collider myCollider;

    void Awake()
    {
        myHealth = GetComponent<EnemyHealth>();
        myAnimator = GetComponentInChildren<Animator>();
        myCollider = GetComponent<Collider>(); 
    }

    void Update()
    {
        if (isGrabbing)
        {
            if (myHealth.isDead || myHealth.isFallen)
            {
                ReleasePlayer(false);
                return;
            }
            if (capturedPlayer != null && grabSnapPoint != null)
            {
                capturedPlayer.transform.position = grabSnapPoint.position;
                capturedPlayer.transform.rotation = grabSnapPoint.rotation;
            }
        }
    }

    // --- FUNÇÃO CHAMADA PELO AI CONTROLLER ---
    public void StartGrabAttempt()
    {
        // Inicia a corrotina de tempo
        StartCoroutine(GrabWindowRoutine());
    }

    private IEnumerator GrabWindowRoutine()
    {
        // 1. Espera o inimigo levantar os braços (Windup)
        canCatchPlayer = false;
        yield return new WaitForSeconds(grabWindupTime);

        // 2. Abre a janela de captura (AGORA VALE!)
        canCatchPlayer = true;
        // Debug.Log("JANELA DE GRAB ABERTA!");

        // 3. Mantém aberta por um tempo
        yield return new WaitForSeconds(grabActiveDuration);

        // 4. Fecha a janela
        canCatchPlayer = false;
        // Debug.Log("JANELA FECHADA.");
    }

    // Chamado pela Hitbox da Mão (que está sempre ativa)
    public void OnHitPlayer(PlayerControllerSystem player)
    {
        // FILTROS LÓGICOS:
        if (isGrabbing) return;           // Já tem um
        if (!canCatchPlayer) return;      // <--- AQUI ESTÁ A MÁGICA: Ignora colisão fora do tempo
        if (player.IsInvulnerable()) return; 

        StartGrab(player);
    }

    private void StartGrab(PlayerControllerSystem player)
    {
        isGrabbing = true;
        canCatchPlayer = false; // Fecha a janela imediatamente pra não pegar 2x
        capturedPlayer = player;

        // Desliga colisão física (pra não explodir)
        Collider playerCol = player.GetComponent<Collider>();
        if (myCollider != null && playerCol != null) Physics.IgnoreCollision(myCollider, playerCol, true);

        player.EnterGrabbedState(this, grabSnapPoint);

        if (myAnimator) myAnimator.SetTrigger(grabSuccessTrigger);

        StartCoroutine(GrabDamageRoutine());
    }

    public void OnGrabBroken()
    {
        if (!isGrabbing) return;
        if (myAnimator) myAnimator.SetTrigger(grabBrokenTrigger);
        ReleasePlayer(true);
    }

    private void ReleasePlayer(bool playerEscaped)
    {
        isGrabbing = false;
        StopAllCoroutines();

        if (capturedPlayer != null)
        {
            Collider playerCol = capturedPlayer.GetComponent<Collider>();
            if (myCollider != null && playerCol != null) Physics.IgnoreCollision(myCollider, playerCol, false);

            capturedPlayer.ExitGrabbedState();
            capturedPlayer = null;
        }
    }

    private IEnumerator GrabDamageRoutine()
    {
        float timer = 0f;
        while (timer < maxGrabDuration && isGrabbing)
        {
            yield return new WaitForSeconds(1.0f);
            timer += 1.0f;
            if (capturedPlayer != null)
            {
                Effect squeeze = new Effect { effectType = Effect.EffectType.physical, power = damagePerSecond };
                capturedPlayer.GetComponent<HealthSystem>().ApplyEffect(new Effect[] { squeeze });
            }
        }
        if (isGrabbing)
        {
            if (myAnimator) myAnimator.SetTrigger(grabFinishTrigger);
            if (capturedPlayer != null)
            {
                Effect bite = new Effect { effectType = Effect.EffectType.physical, power = 20f };
                capturedPlayer.GetComponent<HealthSystem>().ApplyEffect(new Effect[] { bite });
            }
            yield return new WaitForSeconds(1.0f); 
            ReleasePlayer(false);
        }
    }
}