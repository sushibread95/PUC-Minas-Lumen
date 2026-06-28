using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HealthSystem))]
public class PlayerControllerSystem : MonoBehaviour
{
    // ADIÇÃO (expressão de ataque na HUD): disparado quando a personagem ATACA
    // (melee, backstab ou magia). A UI escuta sem o controller conhecê-la.
    public static event System.Action OnPlayerAttacked;

    #region Inspector - References

    [Header("REFERÊNCIAS PRINCIPAIS")]
    [Tooltip("Animator do modelo visual da personagem. Normalmente fica no objeto filho Priscilla.")]
    public Animator animator;

    [Tooltip("Câmera principal usada para calcular movimento relativo à visão.")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("Procura Camera.main automaticamente quando Player Camera estiver vazio.")]
    [SerializeField] private bool autoFindMainCamera = true;

    [Tooltip("Sistema de lock-on usado pelo movimento, câmera e combate.")]
    public LockOnSystem lockOn;

    [Tooltip("Canhão/ponto de disparo usado pelas magias.")]
    [SerializeField] private Cannon cannon;

    private HealthSystem healthSystem;

    #endregion

    #region Inspector - Animation

    [Header("ANIMAÇÃO")]
    [Tooltip("Suavização dos parâmetros enviados ao Animator.")]
    public float animDampTime = 0.1f;

    [Header("ANIMAÇÃO - PARÂMETROS")]
    [Tooltip("Nome do parâmetro Float de velocidade no Animator.")]
    public string speedParam = "Speed";

    [Tooltip("Nome do parâmetro Bool de chão no Animator.")]
    public string groundedParam = "Grounded";

    [Tooltip("Nome do parâmetro Bool de agachamento no Animator.")]
    public string crouchBoolParam = "IsCrouching";

    [Tooltip("Nome do parâmetro Float de movimento lateral no Animator.")]
    public string moveXParam = "MoveX";

    [Tooltip("Nome do parâmetro Float de movimento frontal no Animator.")]
    public string moveYParam = "MoveY";

    private int speedHash, groundedHash, crouchHash, moveXHash, moveYHash;

    #endregion

    #region Inspector - Camera Movement

    [Header("CÂMERA E MOVIMENTO")]
    [Tooltip("Move o player usando a direção horizontal da câmera.")]
    [SerializeField] private bool useCameraBasedMovement = true;

    [Tooltip("Deixe desmarcado com Cinemachine Orbital Follow. O root do Player não deve girar ao andar para não puxar a câmera junto.")]
    [SerializeField] private bool rotatePlayerRootToMoveDirection = false;

    [Tooltip("Velocidade usada apenas se Rotate Player Root To Move Direction estiver ligado.")]
    [SerializeField] private float playerRotationSpeed = 15f;

    #endregion

    #region Inspector - Visual Rotation

    [Header("MODELO VISUAL")]
    [Tooltip("Objeto visual que deve virar ao andar. Use Priscilla, não o Player raiz.")]
    [SerializeField] private Transform characterVisualRoot;

    [Tooltip("Procura automaticamente o objeto do Animator como modelo visual.")]
    [SerializeField] private bool autoFindVisualRoot = true;

    [Tooltip("Faz o modelo visual virar para a direção do movimento.")]
    [SerializeField] private bool rotateVisualToMoveDirection = true;

    [Tooltip("Faz o modelo visual olhar para o alvo durante lock-on.")]
    [SerializeField] private bool rotateVisualToLockOnTarget = true;

    [Tooltip("Força o modelo visual a copiar a rotação do Player. Normalmente deve ficar desmarcado.")]
    [SerializeField] private bool forceVisualRootToFollowPlayerRotation = false;

    [Tooltip("Desliga root motion para impedir a animação de brigar com CharacterController/movimento.")]
    [SerializeField] private bool disableAnimatorRootMotion = true;

    [Tooltip("Velocidade de rotação do modelo visual.")]
    [SerializeField] private float visualRotationSpeed = 20f;

    [Tooltip("Correção de eixo caso o modelo fique virado de lado. Ex.: Y = 90 ou -90.")]
    [SerializeField] private Vector3 visualRotationOffset = Vector3.zero;

    #endregion

    #region Inspector - Movement

    [Header("MOVIMENTO")]
    public float jogSpeed = 4f;
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float sneakSpeed = 1.2f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;
    [Range(0f, 1f)] public float airControl = 0.7f;

    [Header("MOVIMENTO - ALTERNÂNCIAS")]
    public bool toggleSneak = true;
    public bool toggleCrouch = true;
    [Tooltip("Quando desmarcado, Sprint funciona segurando Shift. Quando marcado, Sprint alterna liga/desliga ao apertar.")]
    [SerializeField] private bool toggleSprint = false;

    [Header("MOVIMENTO - BLOQUEIO DE INPUT")]
    [Tooltip("Pequeno atraso ao fechar pause/inventário/diálogo para limpar inputs pressionados durante menus.")]
    [SerializeField] private float inputResumeDelay = 0.12f;

    [Tooltip("Zera sprint e movimento quando gameplay fica bloqueado por pause, inventário, diálogo ou morte.")]
    [SerializeField] private bool resetMovementStateWhenInputBlocked = true;

    [Header("AGACHAR E STEALTH")]
    public float standHeight = 1.6f;
    public float crouchHeight = 0.9f;
    public Vector3 standCenter = new Vector3(0, 0.8f, 0);
    public Vector3 crouchCenter = new Vector3(0, 0.45f, 0);
    public float crouchLerp = 15f;

    [HideInInspector] public float noiseLevel;
    [HideInInspector] public float visibilityFactor;

    #endregion

    #region Inspector - Lock-On Movement

    [Header("LOCK-ON - MOVIMENTO")]
    [Tooltip("Usa movimento relativo ao alvo durante lock-on: W/S aproxima/afasta e A/D faz strafe lateral.")]
    [SerializeField] private bool useLockOnMovement = true;

    [Tooltip("Distância mínima em que W deixa de empurrar o player contra o inimigo.")]
    [SerializeField] private float lockOnCloseStopDistance = 1.65f;

    [Tooltip("Suavização do input durante lock-on para reduzir tremedeira perto do inimigo.")]
    [SerializeField] private float lockOnInputSmooth = 14f;

    [SerializeField] private float lockOnForwardSpeedMultiplier = 1f;
    [SerializeField] private float lockOnBackwardSpeedMultiplier = 0.85f;
    [SerializeField] private float lockOnStrafeSpeedMultiplier = 0.9f;
    [SerializeField] private bool blockForwardWhenTooClose = true;

    #endregion

    #region Inspector - Combat

    [Header("COMBATE - BACKSTAB")]
    public float backstabRange = 1.5f;
    [Range(0f, 1f)] public float backstabAngle = 0.5f;
    public float backstabDamage = 9999f;
    public string backstabTrigger = "Backstab";
    public LayerMask enemyLayer;

    [Header("COMBATE - ARMA")]
    public WeaponItem currentWeaponData;
    public MeleeWeapon equippedWeaponInstance;

    [Header("COMBATE - MAGIAS Q/E/R")]
    [Tooltip("Elemento 0 = Tecla Q | Elemento 1 = Tecla E | Elemento 2 = Tecla R")]
    [SerializeField] private Spell[] spells;

    [Header("COMBATE - DODGE")]
    public string dodgeTriggerParam = "DodgeTrigger";
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.28f;
    public float dodgeCooldown = 0.5f;
    public AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float dodgeLockTime = 0.25f;

    [Header("COMBATE - INVULNERABILIDADE")]
    public float invulnerableDuration = 0.3f;
    private bool _isInvulnerable = false;

    #endregion

    #region Inspector - Audio

    [Header("ÁUDIO - PASSOS")]
    public AudioClip[] walkSounds;
    public AudioClip[] jogSounds;
    public AudioClip[] sneakSounds;
    public float stepInterval = 0.5f;

    #endregion

    #region Inspector - Setup

    [Header("SETUP INICIAL")]
    public bool applyControllerDefaultsOnStart = true;

    [Header("GRAB - CONFIGURAÇÃO")]
    [SerializeField] private float struggleGoal = 100f;

    [HideInInspector] public bool isGrabbed = false;
    [HideInInspector] public float struggleValue = 0f;
    private EnemyGrabber currentGrabber;

    #endregion

    // Internals
    private float nextStepTime = 0f;
    private CharacterController cc;
    private PlayerInputActions input;
    private InputAction sprintAction;

    // Inputs de Magia Separados
    private InputAction castQAction;
    private InputAction castEAction;
    private InputAction castRAction;

    private Vector3 velocity;
    private bool isGrounded;
    private float moveLockUntil = 0f;
    private Coroutine spellRoutine;

    private bool _crouchToggled = false;
    private bool _sneakToggled = false;
    private bool _sprintToggled = false;

    private float _nextDodgeTime = 0f;
    private bool _isDodging = false;
    private float nextSpellTime = 0f;

    private Vector3 visualLookDirection;
    private bool hasVisualLookDirection;
    private Vector2 smoothedLockMoveInput;
    private float inputBlockedUntil = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        healthSystem = GetComponent<HealthSystem>();

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>();

        if (!string.IsNullOrEmpty(speedParam)) speedHash = Animator.StringToHash(speedParam);
        if (!string.IsNullOrEmpty(groundedParam)) groundedHash = Animator.StringToHash(groundedParam);
        if (!string.IsNullOrEmpty(crouchBoolParam)) crouchHash = Animator.StringToHash(crouchBoolParam);
        ResolveVisualRoot();

        if (animator != null && disableAnimatorRootMotion)
            animator.applyRootMotion = false;

        if (!string.IsNullOrEmpty(moveXParam)) moveXHash = Animator.StringToHash(moveXParam);
        if (!string.IsNullOrEmpty(moveYParam)) moveYHash = Animator.StringToHash(moveYParam);
    }

    void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance é NULO.");
            this.enabled = false;
            return;
        }

        ResolvePlayerCamera();
        input = InputManager.Instance.InputActions;

        // Correção: Usando .Get() para garantir acesso ao mapa
        sprintAction = input.Player.Get().FindAction("Sprint");

        // Mapeia as teclas Q, E, R
        castQAction = input.Player.Get().FindAction("CastQ");
        castEAction = input.Player.Get().FindAction("CastE");
        castRAction = input.Player.Get().FindAction("CastR");

        if (applyControllerDefaultsOnStart)
        {
            cc.height = standHeight;
            cc.center = standCenter;
        }

        if (SaveManager.Instance != null) SaveManager.Instance.RegisterPlayer(this.transform);
        
        // Garante que o input está no modo correto
        StartCoroutine(EnsureGameplayInputActive());
    }

    void Update()
    {
        if (IsGameplayInputBlocked())
        {
            HandleGameplayInputBlocked();
            return;
        }

        if (Time.unscaledTime < inputBlockedUntil)
        {
            HandleGameplayInputResumeBuffer();
            return;
        }
        
        // --- CORREÇÃO DE SEGURANÇA (BLINDAGEM) ---
        // Se a cena ativa for "MainMenu", encerramos o Update aqui.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") 
        {
            HandleGameplayInputBlocked();
            return;
        }

        if (isGrabbed)
        {
            HandleGrabInput();
            return; 
        }

        // 2. Controle do Cursor (Alt para liberar)
        bool isAltPressed = Keyboard.current != null && Keyboard.current.altKey.isPressed;
        if (isAltPressed) 
        {
            LockCursor(false);
        }
        else if (lockOn == null || !lockOn.IsLockedOn) 
        {
            LockCursor(true);
        }

        float dt = Time.deltaTime;
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();

        ResolvePlayerCamera();

        // Toggles (Agachar/Esgueirar/Correr)
        if (toggleCrouch) 
        { 
            if (input.Player.Crouch.WasPressedThisFrame()) 
                _crouchToggled = !_crouchToggled; 
        }
        else 
        { 
            _crouchToggled = input.Player.Crouch.IsPressed(); 
        }

        if (toggleSneak) 
        { 
            if (input.Player.SneakSlow.WasPressedThisFrame()) 
                _sneakToggled = !_sneakToggled; 
        }
        else 
        { 
            _sneakToggled = input.Player.SneakSlow.IsPressed(); 
        }

        bool sprintHeld = false;
        if (sprintAction != null)
        {
            if (toggleSprint)
            {
                if (sprintAction.WasPressedThisFrame())
                    _sprintToggled = !_sprintToggled;

                sprintHeld = _sprintToggled;
            }
            else
            {
                _sprintToggled = false;
                sprintHeld = sprintAction.IsPressed();
            }
        }

        // Inputs de Combate
        bool attackMeleePressed = input.Player.AttackB.WasPressedThisFrame();
        bool dodgePressed = input.Player.Jump.WasPressedThisFrame();

        // DODGE
        if (dodgePressed && Time.time >= _nextDodgeTime && !_isDodging)
        {
            Vector3 dodgeDir = GetInputDirection(moveInput);
            if (dodgeDir.sqrMagnitude < 0.0001f) 
                dodgeDir = -transform.forward;
            StartCoroutine(DodgeRoutine(dodgeDir.normalized));
            return;
        }

        // COMBATE HÍBRIDO (Melee + Magias)
        if (attackMeleePressed && Time.time >= nextSpellTime && 
            spellRoutine == null && !_isDodging)
        {
            if (_crouchToggled && TryBackstab())
            {
                // Backstab executado
                OnPlayerAttacked?.Invoke();
            }
            else if (currentWeaponData != null)
            {
                spellRoutine = StartCoroutine(MeleeAttackRoutine());
                OnPlayerAttacked?.Invoke();
            }
        }

        // Magias (Q, E, R)
        if (Time.time >= nextSpellTime && spellRoutine == null && !_isDodging)
        {
            if (castQAction != null && castQAction.WasPressedThisFrame()) 
                TryCastSpellSlot(0);
            else if (castEAction != null && castEAction.WasPressedThisFrame()) 
                TryCastSpellSlot(1);
            else if (castRAction != null && castRAction.WasPressedThisFrame()) 
                TryCastSpellSlot(2);
        }

        // Animator Params (Estados Booleanos)
        if (animator)
        {
            if (groundedHash != 0) 
                animator.SetBool(groundedHash, isGrounded);
            if (crouchHash != 0) 
                animator.SetBool(crouchHash, _crouchToggled);
        }

        // --- MOVIMENTO FÍSICO (A LINHA QUE FALTAVA) ---
        HandleMove(dt, moveInput, _crouchToggled, _sneakToggled, sprintHeld);
        
        // Sons de Passos
        HandleFootsteps(dt, moveInput.magnitude, _crouchToggled, _sneakToggled, sprintHeld);
    }
    void LateUpdate()
    {
        UpdateVisualRootRotation(Time.deltaTime);
    }


    #region Input Block Helpers

    // Verifica se gameplay deve ignorar input por causa de menus, diálogo, morte ou pause.
    private bool IsGameplayInputBlocked()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            return true;

        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
            return true;

        if (CharacterMenuWindow.Instance != null && CharacterMenuWindow.Instance.IsMenuOpen)
            return true;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return true;

        if (DeathScreenManager.Instance != null && DeathScreenManager.Instance.IsDeathScreenActive)
            return true;

        return false;
    }

    // Limpa estados momentâneos para inputs não ficarem presos ao pausar/despausar.
    private void HandleGameplayInputBlocked()
    {
        inputBlockedUntil = Time.unscaledTime + Mathf.Max(0f, inputResumeDelay);

        if (DeathScreenManager.Instance != null && DeathScreenManager.Instance.IsDeathScreenActive)
            LockCursor(false);

        if (!resetMovementStateWhenInputBlocked)
            return;

        ResetTransientMovementState();
    }

    // Mantém o player parado por alguns frames após fechar UI para evitar WasPressedThisFrame acumulado.
    private void HandleGameplayInputResumeBuffer()
    {
        if (resetMovementStateWhenInputBlocked)
            ResetTransientMovementState();
    }

    // Reseta apenas estados transitórios. Não altera agachar/esgueirar se eles forem toggles de gameplay.
    private void ResetTransientMovementState()
    {
        _sprintToggled = false;
        smoothedLockMoveInput = Vector2.zero;
        nextStepTime = 0f;

        velocity.x = 0f;
        velocity.z = 0f;

        if (animator)
        {
            if (speedHash != 0) animator.SetFloat(speedHash, 0f, animDampTime, Time.unscaledDeltaTime);
            if (moveXHash != 0) animator.SetFloat(moveXHash, 0f, animDampTime, Time.unscaledDeltaTime);
            if (moveYHash != 0) animator.SetFloat(moveYHash, 0f, animDampTime, Time.unscaledDeltaTime);
        }
    }

    #endregion

    private IEnumerator EnsureGameplayInputActive()
    {
        // Espera alguns frames para garantir que tudo foi inicializado
        yield return new WaitForSeconds(0.2f);
        
        if (InputManager.Instance == null || input == null) yield break;
        
        // Verifica se o mapa Player está ativo
        bool playerMapActive = input.Player.enabled;
        bool uiMapActive = input.UI.enabled;
        
        // Se estiver no mapa errado, corrige
        if (!playerMapActive && uiMapActive)
        {
            Debug.LogWarning("⚠️ Input estava no mapa UI! Corrigindo para Gameplay...");
            InputManager.Instance.SwitchToGameplayMap();
        }
        
        // Garante cursor travado se não estiver em menu
        if (PauseMenuManager.Instance == null || !PauseMenuManager.Instance.IsPaused)
        {
             LockCursor(true);
        }
    }

    // --- BACKSTAB ---
    private bool TryBackstab()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(origin, transform.forward, out hit, backstabRange, enemyLayer))
        {
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.isDead && !enemy.isFallen)
            {
                float dot = Vector3.Dot(transform.forward, hit.transform.forward);
                if (dot > backstabAngle)
                {
                    Debug.Log("<color=red>BACKSTAB!</color>");
                    if (animator) animator.SetTrigger(backstabTrigger);

                    Effect backstabDmg = new Effect { effectType = Effect.EffectType.physical, power = backstabDamage };
                    enemy.ApplyEffect(new Effect[] { backstabDmg });

                    moveLockUntil = Time.time + 1.5f;
                    nextSpellTime = Time.time + 1.5f;
                    return true;
                }
            }
        }
        return false;
    }

    // --- MAGIA (Verifica Mana e Ativa) ---
    private void TryCastSpellSlot(int slotIndex)
    {
        if (spells != null && slotIndex < spells.Length && spells[slotIndex].projectile != null)
        {
            Spell spellToCast = spells[slotIndex];

            if (healthSystem != null && !healthSystem.CheckEffect(spellToCast.castEffects))
            {
                Debug.Log("Sem Mana para esta magia!");
                return;
            }

            spellRoutine = StartCoroutine(CastSpellRoutine(slotIndex));
            OnPlayerAttacked?.Invoke();
        }
    }

    IEnumerator CastSpellRoutine(int index)
    {
        var spell = spells[index];

        if (spell.castLockTime > 0f)
            moveLockUntil = Mathf.Max(moveLockUntil, Time.time + spell.castLockTime);

        if (animator && !string.IsNullOrEmpty(spell.spellTriggerParam))
            animator.SetTrigger(spell.spellTriggerParam);

        yield return null;

        float measuredLen = 0.3f;
        float timeout = 0.5f;
        while (timeout > 0f)
        {
            var st = animator ? animator.GetCurrentAnimatorStateInfo(spell.spellLayerIndex) : default;
            if (animator && (st.IsName(spell.spellStateName) || animator.GetNextAnimatorStateInfo(spell.spellLayerIndex).IsName(spell.spellStateName)))
            {
                yield return null;
                st = animator.GetCurrentAnimatorStateInfo(spell.spellLayerIndex);
                if (st.length > 0.01f) measuredLen = st.length;
                break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        float fireDelay = measuredLen * 0.2f;
        if (fireDelay > 0f) yield return new WaitForSeconds(fireDelay);

        if (cannon != null) cannon.Fire(spell);

        nextSpellTime = Time.time + measuredLen;
        yield return new WaitForSeconds(measuredLen * 0.8f);

        if (animator && !string.IsNullOrEmpty(spell.spellTriggerParam))
            animator.ResetTrigger(spell.spellTriggerParam);

        spellRoutine = null;
    }

    // --- MELEE (Física) ---
    IEnumerator MeleeAttackRoutine()
    {
        if (currentWeaponData == null) yield break;

        float totalDamage = currentWeaponData.baseDamage;
        if (PlayerStats.Instance != null) totalDamage += PlayerStats.Instance.physicalAttack;

        if (equippedWeaponInstance != null)
        {
            equippedWeaponInstance.Initialize(totalDamage, ProjectileOwner.Player);
        }

        if (animator && !string.IsNullOrEmpty(currentWeaponData.attackTrigger))
            animator.SetTrigger(currentWeaponData.attackTrigger);

        yield return new WaitForSeconds(currentWeaponData.damageWindowStart);

        if (equippedWeaponInstance != null) equippedWeaponInstance.EnableHitbox();

        yield return new WaitForSeconds(currentWeaponData.damageWindowEnd - currentWeaponData.damageWindowStart);

        if (equippedWeaponInstance != null) equippedWeaponInstance.DisableHitbox();

        nextSpellTime = Time.time + currentWeaponData.attackRate;
        spellRoutine = null;
    }

    // ... (Funções de Movimento e Helpers padrão) ...
    private void HandleMove(float dt, Vector2 moveInput, bool crouchHeld, bool sneakHeld, bool sprintHeld)
    {
        if (Time.time < moveLockUntil || _isDodging) moveInput = Vector2.zero;

        Vector3 inputDir;
        float inputMag;
        float lockOnSpeedMultiplier = 1f;

        if (TryBuildLockOnMoveDirection(moveInput, dt, out inputDir, out inputMag, out lockOnSpeedMultiplier))
        {
            // Movimento de lock-on: o player se move em relação ao alvo, sem girar a câmera.
        }
        else
        {
            Vector3 camForward, camRight;
            if (useCameraBasedMovement && playerCamera)
            {
                Vector3 f = playerCamera.transform.forward;
                f.y = 0f;
                camForward = f.sqrMagnitude > 0.001f ? f.normalized : transform.forward;

                Vector3 r = playerCamera.transform.right;
                r.y = 0f;
                camRight = r.sqrMagnitude > 0.001f ? r.normalized : transform.right;
            }
            else
            {
                camForward = transform.forward;
                camRight = transform.right;
            }

            inputDir = camForward * moveInput.y + camRight * moveInput.x;
            inputMag = Mathf.Clamp01(inputDir.magnitude);
        }

        Vector3 desiredMoveDir = inputMag > 0.001f ? inputDir.normalized : Vector3.zero;

        float targetSpeed = walkSpeed;
        visibilityFactor = 1.0f;

        if (crouchHeld) { targetSpeed = Mathf.Min(targetSpeed, crouchSpeed); visibilityFactor = 0.5f; }
        else if (sneakHeld) { targetSpeed = sneakSpeed; visibilityFactor = 0.75f; }
        else if (sprintHeld && inputMag > 0.01f) { targetSpeed = sprintSpeed; visibilityFactor = 1.2f; }

        targetSpeed *= lockOnSpeedMultiplier;
        Vector3 horizMove = desiredMoveDir * (targetSpeed * inputMag);

        // Movimento livre: por padrão gira apenas o modelo visual, não o root do Player.
        // Isso evita que o Cinemachine Orbital Follow receba rotação do alvo e comece a girar junto com o W/A/S/D.
        if ((lockOn == null || !lockOn.IsLockedOn) && inputMag > 0.001f)
        {
            visualLookDirection = desiredMoveDir;
            hasVisualLookDirection = true;

            if (rotatePlayerRootToMoveDirection)
            {
                Quaternion look = Quaternion.LookRotation(desiredMoveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, playerRotationSpeed * dt);
            }
        }

        if (isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * dt;

        Vector3 currentHoriz = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 newHoriz = Vector3.Lerp(currentHoriz, horizMove, (isGrounded ? 1f : airControl) * 10f * dt);
        velocity.x = newHoriz.x; velocity.z = newHoriz.z;

        if (animator)
        {
            float norm = Mathf.Max(0.01f, sprintSpeed);
            Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
            Transform animReference = characterVisualRoot != null ? characterVisualRoot : transform;
            float moveX = Vector3.Dot(animReference.right, horizVel) / norm;
            float moveY = Vector3.Dot(animReference.forward, horizVel) / norm;
            if (speedHash != 0) animator.SetFloat(speedHash, horizVel.magnitude / norm, animDampTime, Time.deltaTime);
            if (moveXHash != 0) animator.SetFloat(moveXHash, moveX, animDampTime, Time.deltaTime);
            if (moveYHash != 0) animator.SetFloat(moveYHash, moveY, animDampTime, Time.deltaTime);
        }

        cc.Move((velocity + Vector3.up * 0) * dt);
        isGrounded = cc.isGrounded;

        float targetH = crouchHeld ? crouchHeight : standHeight;
        float targetY = crouchHeld ? crouchCenter.y : standCenter.y;
        cc.height = Mathf.MoveTowards(cc.height, targetH, crouchLerp * dt);
        cc.center = new Vector3(cc.center.x, Mathf.MoveTowards(cc.center.y, targetY, crouchLerp * dt), cc.center.z);

        float stanceNoiseMod = crouchHeld ? 0.2f : (sneakHeld ? 0.5f : (sprintHeld ? 1.5f : 1.0f));
        noiseLevel = Mathf.Lerp(noiseLevel, inputMag * stanceNoiseMod, dt * 5f);
    }

    #region Lock-On Movement Helpers

    // Cria movimento estável durante lock-on: W/S aproxima/afasta e A/D orbita o alvo.
    private bool TryBuildLockOnMoveDirection(Vector2 rawInput, float dt, out Vector3 inputDir, out float inputMag, out float speedMultiplier)
    {
        inputDir = Vector3.zero;
        inputMag = 0f;
        speedMultiplier = 1f;

        if (!useLockOnMovement || lockOn == null || !lockOn.IsLockedOn || lockOn.CurrentAimPoint == null)
        {
            smoothedLockMoveInput = Vector2.zero;
            return false;
        }

        Vector3 toTarget = lockOn.CurrentAimPoint.position - transform.position;
        toTarget.y = 0f;

        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget < 0.001f)
            return false;

        Vector2 adjustedInput = rawInput;

        // Evita que o W empurre o CharacterController contra o inimigo quando já está perto demais.
        if (blockForwardWhenTooClose && distanceToTarget <= lockOnCloseStopDistance && adjustedInput.y > 0f)
            adjustedInput.y = 0f;

        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, lockOnInputSmooth) * dt);
        smoothedLockMoveInput = Vector2.Lerp(smoothedLockMoveInput, adjustedInput, t);

        Vector3 forwardToTarget = toTarget.normalized;
        Vector3 rightAroundTarget = Vector3.Cross(Vector3.up, forwardToTarget).normalized;

        inputDir = forwardToTarget * smoothedLockMoveInput.y + rightAroundTarget * smoothedLockMoveInput.x;
        inputMag = Mathf.Clamp01(inputDir.magnitude);

        float absX = Mathf.Abs(smoothedLockMoveInput.x);
        float absY = Mathf.Abs(smoothedLockMoveInput.y);

        if (absX > absY)
            speedMultiplier = lockOnStrafeSpeedMultiplier;
        else if (smoothedLockMoveInput.y < -0.01f)
            speedMultiplier = lockOnBackwardSpeedMultiplier;
        else
            speedMultiplier = lockOnForwardSpeedMultiplier;

        return true;
    }

    #endregion

    #region Visual Helpers

    // Encontra automaticamente o objeto visual da personagem, normalmente o filho que possui o Animator.
    private void ResolveVisualRoot()
    {
        if (characterVisualRoot != null || !autoFindVisualRoot)
            return;

        if (animator != null)
            characterVisualRoot = animator.transform;
    }

    // Gira apenas o modelo visual. O root do Player fica estável para não puxar a câmera junto.
    private void UpdateVisualRootRotation(float dt)
    {
        ResolveVisualRoot();

        if (characterVisualRoot == null)
            return;

        Vector3 targetDirection = Vector3.zero;

        if (lockOn != null && lockOn.IsLockedOn && rotateVisualToLockOnTarget && lockOn.CurrentAimPoint != null)
        {
            targetDirection = lockOn.CurrentAimPoint.position - characterVisualRoot.position;
            targetDirection.y = 0f;
        }
        else if (rotateVisualToMoveDirection && hasVisualLookDirection)
        {
            targetDirection = visualLookDirection;
        }
        else if (forceVisualRootToFollowPlayerRotation)
        {
            targetDirection = transform.forward;
        }

        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up) * Quaternion.Euler(visualRotationOffset);
        characterVisualRoot.rotation = Quaternion.Slerp(characterVisualRoot.rotation, targetRotation, visualRotationSpeed * dt);
    }

    #endregion

    #region Camera Helpers

    // Atualiza a referência da câmera principal quando ela está fora do Player ou muda entre cenas.
    private void ResolvePlayerCamera()
    {
        if (playerCamera != null || !autoFindMainCamera)
            return;

        if (Camera.main != null)
            playerCamera = Camera.main;
    }

    // Mantido apenas para compatibilidade: a rotação visual agora é tratada sem girar o root do Player.
    private void RotatePlayerWithCameraView(float dt) { }

    #endregion

    public void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
    public void TeleportToPosition(Vector3 position) { if (cc) { cc.enabled = false; transform.position = position; cc.enabled = true; } }
    
    private Vector3 GetInputDirection(Vector2 moveInput)
    {
        if (useLockOnMovement && lockOn != null && lockOn.IsLockedOn && lockOn.CurrentAimPoint != null)
        {
            Vector3 toTarget = lockOn.CurrentAimPoint.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector3 forwardToTarget = toTarget.normalized;
                Vector3 rightAroundTarget = Vector3.Cross(Vector3.up, forwardToTarget).normalized;
                Vector3 lockDirection = forwardToTarget * moveInput.y + rightAroundTarget * moveInput.x;
                return lockDirection.sqrMagnitude > 0.001f ? lockDirection.normalized : -forwardToTarget;
            }
        }

        Vector3 f = playerCamera ? playerCamera.transform.forward : transform.forward; f.y = 0; f.Normalize();
        Vector3 r = playerCamera ? playerCamera.transform.right : transform.right; r.y = 0; r.Normalize();
        return f * moveInput.y + r * moveInput.x;
    }
    
    IEnumerator DodgeRoutine(Vector3 dir)
    {
        _isDodging = true; _nextDodgeTime = Time.time + dodgeCooldown;
        moveLockUntil = Mathf.Max(moveLockUntil, Time.time + dodgeLockTime);
        if (animator && !string.IsNullOrEmpty(dodgeTriggerParam)) animator.SetTrigger(dodgeTriggerParam);
        velocity.y = 0f; StartCoroutine(Invulnerability(invulnerableDuration));
        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            float t = Mathf.Clamp01(elapsed / dodgeDuration);
            Vector3 step = dir * (dodgeDistance * dodgeSpeedCurve.Evaluate(t) * Time.deltaTime);
            cc.Move(step); elapsed += Time.deltaTime; yield return null;
        }
        _isDodging = false;
    }

    private void HandleGrabInput()
    {
        // Player esmaga o botão de Pulo (Espaço/A) ou Interagir (F/X) para soltar
        if (input.Player.Jump.WasPressedThisFrame() || input.Player.Interact.WasPressedThisFrame())
        {
            struggleValue += 15f; // Dificuldade: Quanto cada clique enche a barra
            
            // Opcional: Tocar som de esforço
            // Opcional: Tremida na câmera

            if (struggleValue >= struggleGoal)
            {
                // VENCEU!
                if (currentGrabber != null) currentGrabber.OnGrabBroken();
                ExitGrabbedState();
            }
        }
    }

    public void EnterGrabbedState(EnemyGrabber grabber, Transform snapPoint)
    {
        isGrabbed = true;
        currentGrabber = grabber;
        struggleValue = 0f;
        
        // Desliga física de movimento para não brigar com o inimigo
        if (cc) cc.enabled = false; 

        // Teleporta para a posição exata da animação (Snap)
        transform.position = snapPoint.position;
        transform.rotation = snapPoint.rotation;

        // Toca animação de "Sendo Segurado" (se tiver) ou Idle
        if (animator) animator.SetBool("IsGrabbed", true); // Crie esse Bool no Animator do Player!
    }

    public void ExitGrabbedState()
    {
        isGrabbed = false;
        currentGrabber = null;
        if (cc) cc.enabled = true;
        if (animator) animator.SetBool("IsGrabbed", false);
    }

    IEnumerator Invulnerability(float duration) { _isInvulnerable = true; yield return new WaitForSeconds(duration); _isInvulnerable = false; }
    public bool IsInvulnerable() => _isInvulnerable;
    
    private void HandleFootsteps(float deltaTime, float inputMagnitude, bool isCrouching, bool isSneaking, bool isSprinting)
    {
        if (inputMagnitude > 0.1f && isGrounded && Time.time >= nextStepTime)
        {
            // 1. Seleciona o "Pool" (Lista) de sons correto para o estado atual
            AudioClip[] currentPool = null;

            if (isCrouching || isSneaking) currentPool = sneakSounds;
            else if (isSprinting) currentPool = jogSounds;
            else currentPool = walkSounds;

            // 2. Se a lista tiver sons, sorteia um e toca
            if (currentPool != null && currentPool.Length > 0)
            {
                // Sorteia um índice aleatório (0, 1 ou 2...)
                int randomIndex = Random.Range(0, currentPool.Length);
                AudioClip clip = currentPool[randomIndex];

                if (AudioManager.Instance && clip) 
                    AudioManager.Instance.PlaySFX(clip, transform.position);
            }

            // 3. Calcula o tempo para o próximo passo
            float interval = stepInterval * (isCrouching ? 1.8f : (isSprinting ? 0.6f : 1f));
            nextStepTime = Time.time + interval;
        }
        else if (!isGrounded || inputMagnitude < 0.1f) 
        {
            nextStepTime = 0f;
        }
    }

    public void InterruptActions()
    {
        // 1. Para qualquer ataque ou magia que esteja acontecendo
        if (spellRoutine != null)
        {
            StopCoroutine(spellRoutine);
            spellRoutine = null;
        }

        // 2. Desliga a Hitbox FORÇADAMENTE (Previne o Bug do Sabre de Luz)
        if (equippedWeaponInstance != null) 
        {
            equippedWeaponInstance.DisableHitbox();
        }

        // 3. Destrava o movimento do personagem
        _isDodging = false;
        moveLockUntil = 0f;
        
        // Opcional: Reseta os triggers de ataque no Animator aqui se precisar
    }
}