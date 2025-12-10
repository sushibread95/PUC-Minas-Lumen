using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HealthSystem))]
public class PlayerControllerSystem : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public float animDampTime = 0.1f;

    [Header("Animation Params")]
    public string speedParam = "Speed";
    public string groundedParam = "Grounded";
    public string crouchBoolParam = "IsCrouching";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    private int speedHash, groundedHash, crouchHash, moveXHash, moveYHash;

    [Header("Refs")]
    private Camera playerCamera;
    public LockOnSystem lockOn;

    [Header("Move")]
    public float jogSpeed = 4f;
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float sneakSpeed = 1.2f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;
    [Range(0f, 1f)] public float airControl = 0.7f;

    [Header("Toggles")]
    public bool toggleSneak = true;
    public bool toggleCrouch = true;

    [Header("Crouch & Stealth (Base 1.60m)")]
    public float standHeight = 1.6f;
    public float crouchHeight = 0.9f;
    public Vector3 standCenter = new Vector3(0, 0.8f, 0);
    public Vector3 crouchCenter = new Vector3(0, 0.45f, 0);
    public float crouchLerp = 15f;

    [Header("Stealth Data (Read-Only)")]
    public float noiseLevel;
    public float visibilityFactor;

    [Header("Backstab / Stealth Kill")]
    public float backstabRange = 1.5f;
    [Range(0f, 1f)] public float backstabAngle = 0.5f;
    public float backstabDamage = 9999f;
    public string backstabTrigger = "Backstab";
    public LayerMask enemyLayer;

    [Header("Melee Combat")]
    public WeaponItem currentWeaponData;
    public MeleeWeapon equippedWeaponInstance;

    [Header("Spells (Slots Q, E, R)")]
    [Tooltip("Elemento 0 = Tecla Q | Elemento 1 = Tecla E | Elemento 2 = Tecla R")]
    [SerializeField] private Spell[] spells;

    [Header("Components")]
    [SerializeField] private Cannon cannon;
    private HealthSystem healthSystem;

    [Header("Dodge")]
    public string dodgeTriggerParam = "DodgeTrigger";
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.28f;
    public float dodgeCooldown = 0.5f;
    public AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float dodgeLockTime = 0.25f;

    [Header("Invulnerability")]
    public float invulnerableDuration = 0.3f;
    private bool _isInvulnerable = false;

    [Header("Footstep Settings")]
    public AudioClip walkSound;
    public AudioClip jogSound;
    public AudioClip sneakSound;
    public float stepInterval = 0.5f;

    [Header("Controller Defaults")]
    public bool applyControllerDefaultsOnStart = true;

    [Header("Status - Grabbed")]
    public bool isGrabbed = false;
    public float struggleValue = 0f;
    public float struggleGoal = 100f;
    private EnemyGrabber currentGrabber;

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

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        healthSystem = GetComponent<HealthSystem>();

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>();

        if (!string.IsNullOrEmpty(speedParam)) speedHash = Animator.StringToHash(speedParam);
        if (!string.IsNullOrEmpty(groundedParam)) groundedHash = Animator.StringToHash(groundedParam);
        if (!string.IsNullOrEmpty(crouchBoolParam)) crouchHash = Animator.StringToHash(crouchBoolParam);
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

        playerCamera = GetComponentInChildren<Camera>();
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
    }

    void Update()
    {
        // 1. Cláusula de Guarda
        if (input == null ||
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen) ||
           (CharacterMenuWindow.Instance != null && CharacterMenuWindow.Instance.IsMenuOpen) ||
           (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            return;
        }

        // 2. Mouse (Alt)
        
        // --- CORREÇÃO DE SEGURANÇA (BLINDAGEM) ---
        // Se a cena ativa for "MainMenu", encerramos o Update aqui.
        // Isso impede que o Player trave o cursor ou processe movimento se ele persistir por engano no Menu.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") 
        {
            return;
        }

        if (isGrabbed)
        {
            HandleGrabInput();
            return; // TRAVA TUDO: Não anda, não ataca, não abre menu.
        }
        // ------------------------------------------

        bool isAltPressed = Keyboard.current != null && Keyboard.current.altKey.isPressed;
        if (isAltPressed) LockCursor(false);
        else if (lockOn == null || !lockOn.IsLockedOn) LockCursor(true);

        float dt = Time.deltaTime;
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();

        // Toggles
        if (toggleCrouch) { if (input.Player.Crouch.WasPressedThisFrame()) _crouchToggled = !_crouchToggled; }
        else { _crouchToggled = input.Player.Crouch.IsPressed(); }

        if (toggleSneak) { if (input.Player.SneakSlow.WasPressedThisFrame()) _sneakToggled = !_sneakToggled; }
        else { _sneakToggled = input.Player.SneakSlow.IsPressed(); }

        bool sprintHeld = false;
        if (sprintAction != null)
        {
            if (sprintAction.WasPressedThisFrame()) _sprintToggled = !_sprintToggled;
            sprintHeld = _sprintToggled;
        }

        // Inputs de Combate
        bool attackMeleePressed = input.Player.AttackB.WasPressedThisFrame();
        bool dodgePressed = input.Player.Jump.WasPressedThisFrame();

        // DODGE
        if (dodgePressed && Time.time >= _nextDodgeTime && !_isDodging)
        {
            Vector3 dodgeDir = GetInputDirection(moveInput);
            if (dodgeDir.sqrMagnitude < 0.0001f) dodgeDir = -transform.forward;
            StartCoroutine(DodgeRoutine(dodgeDir.normalized));
            return;
        }

        // MOVIMENTO E STEALTH
        HandleMove(dt, moveInput, _crouchToggled, _sneakToggled, sprintHeld);

        // --- COMBATE HÍBRIDO (Melee Mouse + Magias Q/E/R) ---

        // 1. Ataque Melee / Backstab (Botão Esquerdo)
        if (attackMeleePressed && Time.time >= nextSpellTime && spellRoutine == null && !_isDodging)
        {
            // Prioridade: Tenta Backstab se estiver agachado
            if (_crouchToggled && TryBackstab())
            {
                // Se deu certo, o TryBackstab já tocou animação e aplicou dano
            }
            // Se não, ataque normal com arma
            else if (currentWeaponData != null)
            {
                spellRoutine = StartCoroutine(MeleeAttackRoutine());
            }
            else
            {
                Debug.Log("Sem arma equipada. (Adicione lógica de soco se quiser)");
            }
        }

        // 2. Magias (Q, E, R)
        if (Time.time >= nextSpellTime && spellRoutine == null && !_isDodging)
        {
            if (castQAction != null && castQAction.WasPressedThisFrame()) TryCastSpellSlot(0);
            else if (castEAction != null && castEAction.WasPressedThisFrame()) TryCastSpellSlot(1);
            else if (castRAction != null && castRAction.WasPressedThisFrame()) TryCastSpellSlot(2);
        }

        // Animator Params
        if (animator)
        {
            if (groundedHash != 0) animator.SetBool(groundedHash, isGrounded);
            if (crouchHash != 0) animator.SetBool(crouchHash, _crouchToggled);
        }

        HandleFootsteps(dt, moveInput.magnitude, _crouchToggled, _sneakToggled, sprintHeld);


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

        Vector3 camForward, camRight;
        if (playerCamera)
        {
            Vector3 f = playerCamera.transform.forward; f.y = 0f; camForward = f.normalized;
            Vector3 r = playerCamera.transform.right; r.y = 0f; camRight = r.normalized;
        }
        else { camForward = transform.forward; camRight = transform.right; }

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        float inputMag = Mathf.Clamp01(inputDir.magnitude);

        float targetSpeed = walkSpeed;
        visibilityFactor = 1.0f;

        if (crouchHeld) { targetSpeed = Mathf.Min(targetSpeed, crouchSpeed); visibilityFactor = 0.5f; }
        else if (sneakHeld) { targetSpeed = sneakSpeed; visibilityFactor = 0.75f; }
        else if (sprintHeld && inputMag > 0.01f) { targetSpeed = sprintSpeed; visibilityFactor = 1.2f; }

        Vector3 horizMove = inputDir.normalized * (targetSpeed * inputMag);

        if ((lockOn == null || !lockOn.IsLockedOn) && inputMag > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 15f * dt);
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
            float moveX = Vector3.Dot(transform.right, horizVel) / norm;
            float moveY = Vector3.Dot(transform.forward, horizVel) / norm;
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

    public void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
    public void TeleportToPosition(Vector3 position) { if (cc) { cc.enabled = false; transform.position = position; cc.enabled = true; } }
    private Vector3 GetInputDirection(Vector2 moveInput)
    {
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
            AudioClip clip = isCrouching ? sneakSound : (isSprinting ? jogSound : (isSneaking ? sneakSound : walkSound));
            float interval = stepInterval * (isCrouching ? 1.8f : (isSprinting ? 0.6f : 1f));
            if (AudioManager.Instance && clip) AudioManager.Instance.PlaySFX(clip, transform.position);
            nextStepTime = Time.time + interval;
        }
        else if (!isGrounded || inputMagnitude < 0.1f) nextStepTime = 0f;
    }
}