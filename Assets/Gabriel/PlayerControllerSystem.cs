// Nome do arquivo: PlayerControllerSystem.cs
// CÓDIGO COMPLETO (COM A CORREÇÃO DE ERROS CS0103)

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.InputSystem.Controls;   // KeyControl

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerSystem : MonoBehaviour
{
    // ... (todo o seu código de Header/variáveis permanece o mesmo) ...
    // ... (animator, speed, refs, move, toggles, crouch, etc) ...

    [Header("Animation")]
    public Animator animator;
    public float animDampTime = 0.1f;

    [Header("Animation Params (names in Animator)")]
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
    public float sneakSpeed = 1f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;
    [Range(0f, 1f)] public float airControl = 0.7f;

    [Header("Toggles")]
    public bool toggleSneak = true;
    public bool toggleCrouch = true;

    [Header("Crouch (CharacterController)")]
    public float standHeight = 1.8f;
    public float crouchHeight = 1.2f;
    public Vector3 standCenter = new Vector3(0, 0.9f, 0);
    public Vector3 crouchCenter = new Vector3(0, 0.6f, 0);
    public float crouchLerp = 24f;

    [Header("Controller Defaults")]
    public bool applyControllerDefaultsOnStart = false;

    [Header("Animation Mapping")]
    [Range(0f, 1f)] public float walkBlendPoint = 0.5f;

    [Header("Noise/Stealth (read-only)")]
    public float noiseLevel;

    [Header("Spells")]
    [SerializeField] private Spell[] spells;
    [Header("Components")]
    [SerializeField] private Cannon cannon;

    [Header("Dodge (Dark Souls style)")]
    public string dodgeTriggerParam = "DodgeTrigger";
    public string dodgeStateName = "Dodge";
    public int    dodgeLayerIndex = 0;
    public float  dodgeDistance = 4f;
    public float  dodgeDuration = 0.28f;
    public float  dodgeCooldown = 0.5f;
    public AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0,1,1,0);
    public float  dodgeLockTime = 0.25f;

    [Header("Invulnerability (i-frames)")]
    [Tooltip("Duração de invencibilidade during dodge (s)")] public float invulnerableDuration = 0.3f;
    private bool _isInvulnerable = false;

    [Header("Footstep Settings")]
    public AudioClip walkSound;
    public AudioClip jogSound;
    public AudioClip sneakSound;
    public float stepInterval = 0.5f; // Tempo entre os passos (em segundos)

    // internals
    private float nextStepTime = 0f;
    private CharacterController cc;
    private PlayerInputActions input;
    private InputAction sprintAction; 

    private Vector3 velocity;                    
    private bool isGrounded;
    private float moveLockUntil = 0f;            
    private Coroutine spellRoutine;              

    private bool _crouchToggled = false;
    private bool _sneakToggled = false;
    private bool _sprintToggled = false; 

    private float _nextDodgeTime = 0f;
    private bool _isDodging = false;

    private int currentAttackSlot = 0;
    private float nextSpellTime = 0f;


    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (cannon == null) cannon = GetComponentInChildren<Cannon>();

        if (!string.IsNullOrEmpty(speedParam))      speedHash = Animator.StringToHash(speedParam);
        if (!string.IsNullOrEmpty(groundedParam))   groundedHash = Animator.StringToHash(groundedParam);
        if (!string.IsNullOrEmpty(crouchBoolParam)) crouchHash = Animator.StringToHash(crouchBoolParam);
        if (!string.IsNullOrEmpty(moveXParam))      moveXHash = Animator.StringToHash(moveXParam);
        if (!string.IsNullOrEmpty(moveYParam))      moveYHash = Animator.StringToHash(moveYParam);
    }

    void Start()
    {
        // Pega a referência do InputManager (que rodou no Awake da cena Boot)
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance é NULO. O PlayerController não consegue pegar os inputs.");
            this.enabled = false;
            return; 
        }
        
        // A Câmera é procurada no filho do Prefab
        playerCamera = GetComponentInChildren<Camera>();
        if (!playerCamera) 
        {
            Debug.LogError("PlayerControllerSystem: Câmera não encontrada como filha do Player Prefab. O jogo não vai renderizar.");
        }
        
        // Pega a "Fonte da Verdade" do Input
        input = InputManager.Instance.InputActions;

        // Pega as ações
        sprintAction = input.FindAction("Sprint", false);

        // Se inscreve em eventos de input
        if (input.Player.SelectAttack != null)
            input.Player.SelectAttack.performed += OnSelectAttackPerformed;
        
        // Configurações padrão do Controller
        if (applyControllerDefaultsOnStart)
        {
            cc.height = standHeight;
            cc.center = standCenter;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterPlayer(this.transform);
        }
        else
        {
            Debug.LogWarning("PlayerController tentou se registrar, mas o SaveManager.Instance ainda não existe.");
        }
    }
    
    private void OnDestroy()
    {
        // Se desinscreve dos eventos para evitar erros
        if (input != null && InputManager.Instance != null && input.Player.SelectAttack != null)
            input.Player.SelectAttack.performed -= OnSelectAttackPerformed;
    }
    
    void Update()
    {
        // --- CLÁUSULA DE GUARDA MESTRA (INTEGRADA) ---
        if (input == null || 
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
        {
            return; 
        }
        // ---------------------------------------------

        // Trava o cursor durante o gameplay (se não estiver em LockOn)
        if (lockOn == null || !lockOn.IsLockedOn)
        {
            LockCursor(true); 
        }

        float dt = Time.deltaTime;

        // INPUTS
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>(); 
        
        // Lógica de Crouch
        if (toggleCrouch) {
            if (input.Player.Crouch.WasPressedThisFrame()) _crouchToggled = !_crouchToggled;
        } else { _crouchToggled = input.Player.Crouch.IsPressed(); }
        bool crouch = _crouchToggled;

        // Lógica de Sneak
        if (toggleSneak) {
            if (input.Player.SneakSlow.WasPressedThisFrame()) _sneakToggled = !_sneakToggled;
        } else { _sneakToggled = input.Player.SneakSlow.IsPressed(); }
        bool sneak = _sneakToggled;

        // Lógica de Sprint
        bool sprintHeld = false;
        if (sprintAction != null) {
            if (sprintAction.WasPressedThisFrame()) _sprintToggled = !_sprintToggled;
            sprintHeld = _sprintToggled;
        }

        bool attackPressed    = input.Player.AttackB.WasPressedThisFrame();
        bool dodgePressed     = input.Player.Jump.WasPressedThisFrame(); 
        
        // Lógica de DODGE
        if (dodgePressed && Time.time >= _nextDodgeTime && !_isDodging)
        {
            Vector3 dodgeDir = GetInputDirection(moveInput);
            if (dodgeDir.sqrMagnitude < 0.0001f) dodgeDir = -transform.forward; 
            StartCoroutine(DodgeRoutine(dodgeDir.normalized));
            return; 
        }

        // Lógica de Movimento
        HandleMove(dt, moveInput, crouch, sneak, sprintHeld);

        // Lógica de Ataque
        if (attackPressed && Time.time >= nextSpellTime && spellRoutine == null && !_isDodging)
        {
            if (spells != null && spells.Length > 0)
            {
                int idx = Mathf.Clamp(currentAttackSlot, 0, spells.Length - 1);
                spellRoutine = StartCoroutine(CastSpellRoutine(idx));
            }
        }
        
        // Lógica de Animator
        if (animator)
        {
            if (groundedHash != 0) animator.SetBool(groundedHash, isGrounded);
            if (crouchHash   != 0) animator.SetBool(crouchHash, crouch);
        }

        // --- CORREÇÃO AQUI: Passando moveInput.magnitude e sprintHeld ---
        HandleFootsteps(dt, moveInput.magnitude, crouch, sneak, sprintHeld);
    }

    // --- CORREÇÃO AQUI: Adicionado parâmetro 'bool isSprinting' ---
    void HandleFootsteps(float deltaTime, float inputMagnitude, bool isCrouching, bool isSneaking, bool isSprinting)
    {
        if (inputMagnitude > 0.1f && isGrounded && Time.time >= nextStepTime)
        {
            AudioClip clipToPlay = null;
            float currentStepInterval = stepInterval;

            if (isCrouching || isSneaking)
            {
                clipToPlay = sneakSound;
                currentStepInterval *= 1.5f; 
            }
            // --- CORREÇÃO AQUI: Usando o novo parâmetro 'isSprinting' ---
            else if (isSprinting) 
            {
                clipToPlay = jogSound;
                currentStepInterval *= 0.75f; // Deixa o passo mais rápido
            }
            else
            {
                clipToPlay = walkSound;
            }

            if (AudioManager.Instance != null && clipToPlay != null)
            {
                AudioManager.Instance.PlaySFX(clipToPlay, transform.position);
            }

            // Reseta o timer
            nextStepTime = Time.time + currentStepInterval;
        }
        else if (!isGrounded || inputMagnitude < 0.1f)
        {
            // Se o player não está no chão ou não está se movendo, reseta o timer
            nextStepTime = 0f;
        }
    }

    // Função de travar o cursor (o "Vilão" anterior, agora controlado)
    public void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // --- FUNÇÃO DE TELEPORT (Para o SaveManager) ---
    public void TeleportToPosition(Vector3 position)
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false; 
            transform.position = position; 
            cc.enabled = true; 
            Debug.LogWarning("Player teleportado para a posição salva: " + position);
        }
    }
    
    // --- Funções de Lógica de Jogo (GetInputDirection, Dodge, Cast, HandleMove, etc.) ---

    private Vector3 GetInputDirection(Vector2 moveInput)
    {
        Vector3 camForward, camRight;
        if (playerCamera)
        {
            Vector3 f = playerCamera.transform.forward; f.y = 0f; camForward = f.normalized;
            Vector3 r = playerCamera.transform.right;   r.y = 0f; camRight   = r.normalized;
        }
        else { camForward = transform.forward; camRight = transform.right; }

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        return inputDir;
    }

    IEnumerator DodgeRoutine(Vector3 dir)
    {
        _isDodging = true;
        _nextDodgeTime = Time.time + dodgeCooldown;
        moveLockUntil = Mathf.Max(moveLockUntil, Time.time + dodgeLockTime);
        if (animator && !string.IsNullOrEmpty(dodgeTriggerParam))
            animator.SetTrigger(dodgeTriggerParam);
        velocity.y = 0f;
        StartCoroutine(Invulnerability(invulnerableDuration));
        float elapsed = 0f;
        Vector3 horizVelBefore = new Vector3(velocity.x, 0, velocity.z);
        while (elapsed < dodgeDuration)
        {
            float t = Mathf.Clamp01(elapsed / dodgeDuration);
            float speed01 = dodgeSpeedCurve.Evaluate(t);
            float distStep = (dodgeDistance * speed01) * Time.deltaTime;
            Vector3 step = dir * distStep;
            cc.Move(step);
            elapsed += Time.deltaTime;
            yield return null;
        }
        velocity.x = horizVelBefore.x;
        velocity.z = horizVelBefore.z;
        _isDodging = false;
    }

    IEnumerator Invulnerability(float duration)
    {
        _isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        _isInvulnerable = false;
    }

    public bool IsInvulnerable() => _isInvulnerable;

    IEnumerator CastSpellRoutine(int index)
    {
        if (spells == null || index < 0 || index >= spells.Length) yield break;
        var spell = spells[index];
        if (Time.time < nextSpellTime) yield break;
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
        if (cannon != null)
            cannon.Fire(spell);
        else
            Debug.LogWarning("PlayerControllerSystem: Cannon não encontrado para disparar o spell.");
        nextSpellTime = Time.time + measuredLen;
        yield return new WaitForSeconds(measuredLen * 0.8f);
        if (animator && !string.IsNullOrEmpty(spell.spellTriggerParam))
            animator.ResetTrigger(spell.spellTriggerParam);
        spellRoutine = null;
    }

    private void HandleMove(float dt, Vector2 moveInput, bool crouchHeld, bool sneakHeld, bool sprintHeld)
    {
        if (Time.time < moveLockUntil || _isDodging)
        {
            moveInput = Vector2.zero;
        }
        Vector3 camForward, camRight;
        
        if (playerCamera)
        {
            Vector3 f = playerCamera.transform.forward; f.y = 0f; camForward = f.normalized;
            Vector3 r = playerCamera.transform.right;   r.y = 0f; camRight   = r.normalized;
        }
        else { camForward = transform.forward; camRight = transform.right; }

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        float inputMag = Mathf.Clamp01(inputDir.magnitude);
        float targetSpeed = walkSpeed;
        bool hasMoveInput = inputMag > 0.01f;
        if (sprintHeld && hasMoveInput && !crouchHeld)
            targetSpeed = sprintSpeed;
        else if (sneakHeld)
            targetSpeed = sneakSpeed;
        if (crouchHeld)
            targetSpeed = Mathf.Min(targetSpeed, crouchSpeed);
        Vector3 horizMove = inputDir.normalized * (targetSpeed * inputMag);
        if ((lockOn == null || !lockOn.IsLockedOn) && inputMag > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 15f * dt);
        }
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * dt;
        float control = isGrounded ? 1f : airControl;
        Vector3 currentHoriz = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 desiredHoriz = horizMove;
        Vector3 newHoriz = Vector3.Lerp(currentHoriz, desiredHoriz, control * 10f * dt);
        velocity.x = newHoriz.x;
        velocity.z = newHoriz.z;
        if (animator)
        {  
            Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
            float norm = Mathf.Max(0.01f, sprintSpeed);
            float moveX = Vector3.Dot(transform.right,  horizVel) / norm;
            float moveY = Vector3.Dot(transform.forward, horizVel) / norm;
            if (speedHash != 0)
            {
                float speed01 = horizVel.magnitude / norm;
                animator.SetFloat(speedHash, speed01, animDampTime, Time.deltaTime);
            }
            if (moveXHash != 0) animator.SetFloat(moveXHash, moveX, animDampTime, Time.deltaTime);
            if (moveYHash != 0) animator.SetFloat(moveYHash, moveY, animDampTime, Time.deltaTime);
        }
        Vector3 displacement = (new Vector3(velocity.x, 0f, velocity.z) + Vector3.up * velocity.y) * dt;
        cc.Move(displacement);
        isGrounded = cc.isGrounded;
        float targetH = crouchHeld ? crouchHeight : standHeight;
        float targetY = crouchHeld ? crouchCenter.y : standCenter.y;
        cc.height = Mathf.MoveTowards(cc.height, targetH, crouchLerp * dt);
        cc.center = new Vector3(cc.center.x, Mathf.MoveTowards(cc.center.y, targetY, crouchLerp * dt), cc.center.z);
        noiseLevel = Mathf.Lerp(noiseLevel, inputMag * (crouchHeld ? 0.3f : sneakHeld ? 0.5f : 1f), dt * 5f);
    }

    private void OnSelectAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        var control = ctx.control;
        if (control == null) return;
        if (control is KeyControl key)
        {
            if      (key.keyCode == Key.Digit1 || key.keyCode == Key.Numpad1) currentAttackSlot = 0;
            else if (key.keyCode == Key.Digit2 || key.keyCode == Key.Numpad2) currentAttackSlot = 1;
            else if (key.keyCode == Key.Digit3 || key.keyCode == Key.Numpad3) currentAttackSlot = 2;
            else if (key.keyCode == Key.Digit4 || key.keyCode == Key.Numpad4) currentAttackSlot = 3;
            else if (key.keyCode == Key.Digit5 || key.keyCode == Key.Numpad5) currentAttackSlot = 4;
            else if (key.keyCode == Key.Q || key.keyCode == Key.LeftBracket)  CycleSpell(-1);
            else if (key.keyCode == Key.E || key.keyCode == Key.RightBracket) CycleSpell(+1);
            ClampSlot();
            return;
        }
        if (control.device is Gamepad gp)
        {
            if      (control == gp.dpad.up)    currentAttackSlot = 0;
            else if (control == gp.dpad.left)  currentAttackSlot = 1;
            else if (control == gp.dpad.down)  currentAttackSlot = 2;
            else if (control == gp.dpad.right) currentAttackSlot = 3;
            else if (control == gp.leftShoulder)  CycleSpell(-1);
            else if (control == gp.rightShoulder) CycleSpell(+1);
            ClampSlot();
            return;
        }
    }

    private void CycleSpell(int dir)
    {
        if (spells == null || spells.Length == 0) { currentAttackSlot = 0; return; }
        currentAttackSlot = (currentAttackSlot + dir) % spells.Length;
        if (currentAttackSlot < 0) currentAttackSlot += spells.Length;
    }

    private void ClampSlot()
    {
        if (spells == null || spells.Length == 0) { currentAttackSlot = 0; return; }
        currentAttackSlot = Mathf.Clamp(currentAttackSlot, 0, spells.Length - 1);
    }
}