using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;                 
    public float animDampTime = 0.1f;         

    [Header("Animation Params (names in Animator)")]
    [Tooltip("Float usado no BlendTree Locomotion (0..1).")]
    public string speedParam = "Speed";
    [Tooltip("Bool que marca se está no chão (opcional).")]
    public string groundedParam = "Grounded";
    [Tooltip("Bool para entrar/sair do crouch (usa-se nas Conditions das transições).")]
    public string crouchBoolParam = "IsCrouching";
    [Tooltip("Trigger do pulo no Animator (Any State -> Jump).")]
    public string jumpTriggerParam = "JumpTrigger";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    // hashes (evitam lookup por string todo frame)
    int speedHash = -1, groundedHash = -1, crouchHash = -1, spellStateHash = -1, moveXHash = -1, moveYHash = -1;

    [Header("Refs")]
    public Camera playerCamera; // Cinemachine Brain estará na Main Camera

    [Header("Move")]
    public float walkSpeed = 3.5f;
    public float sneakSpeed = 1.2f;     // Shift (andar devagar)
    public float crouchSpeed = 2.0f;    // enquanto agachado
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    [Range(0f, 1f)] public float airControl = 0.7f; // controle no ar (horizontal)

    [Header("Crouch (CharacterController)")]
    public float standHeight = 1.8f;
    public float crouchHeight = 1.2f;
    public Vector3 standCenter = new Vector3(0f, 0.9f, 0f);
    public Vector3 crouchCenter = new Vector3(0f, 0.6f, 0f);
    public float crouchLerp = 24f;      // mais rápido para acompanhar animações snappy

    [Header("Controller Defaults")]
    [Tooltip("Se true, aplica standHeight/standCenter no Start(). Se false, usa o CC atual como baseline.")]
    public bool applyControllerDefaultsOnStart = false;

    [Header("Animation Mapping")]
    [Tooltip("Valor alvo do BlendTree para caminhar (Idle=0, Walk=walkBlendPoint, Run=1).")]
    [Range(0f, 1f)] public float walkBlendPoint = 0.5f;

    [Header("Noise/Stealth (read-only)")]
    public float noiseLevel;

    // -------- Spell / Attack 1 --------
    [Header("Spell / Attack 1 (Purificação)")]
    [Tooltip("Trigger do Animator para o cast (Any State -> Spell_Cast)")]
    public string spellTriggerParam = "CastTrigger";
    [Tooltip("Nome do estado de cast no Animator (curto, ex.: 'Spell_Cast'). Usado para medir a duração (cooldown).")]
    public string spellStateName = "Spell_Cast";
    [Tooltip("Índice da layer onde o estado de cast está (0 = Base Layer)")]
    public int spellLayerIndex = 0;
    [Tooltip("Trava leve opcional de movimento/jump durante o início do cast (0 = sem trava)")]
    public float castLockTime = 0f;
    [Tooltip("Pivô opcional para spawnar VFX/projétil via script (pode usar Animation Event também)")]
    public Transform spellMuzzle;
    public GameObject spellVfxPrefab;

    // internals
    CharacterController cc;
    PlayerInputActions input;
    InputAction inventoryAction;         // opcional (LB), null se não existir
    InputAction cameraResetAction;       // opcional: buscada por nome

    Vector3 velocity;                    // y acumula gravidade/pulo
    bool isGrounded;
    int currentAttackSlot = 1;

    float nextSpellTime = 0f;
    Coroutine spellRoutine;
    float moveLockUntil = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        input = new PlayerInputActions();

        if (!animator) animator = GetComponentInChildren<Animator>();

        // ações opcionais; se não existirem, ficam null e usamos fallback
        inventoryAction   = input.FindAction("Inventory", false);

        // SelectAttack via evento (um único action com vários bindings)
        if (input.Player.SelectAttack != null)
            input.Player.SelectAttack.performed += OnSelectAttackPerformed;

        // computa hashes uma vez
        if (!string.IsNullOrEmpty(speedParam))      speedHash = Animator.StringToHash(speedParam);
        if (!string.IsNullOrEmpty(groundedParam))   groundedHash = Animator.StringToHash(groundedParam);
        if (!string.IsNullOrEmpty(crouchBoolParam)) crouchHash = Animator.StringToHash(crouchBoolParam);
        if (!string.IsNullOrEmpty(spellStateName))  spellStateHash = Animator.StringToHash(spellStateName);
        if (!string.IsNullOrEmpty(moveXParam)) moveXHash = Animator.StringToHash(moveXParam);
        if (!string.IsNullOrEmpty(moveYParam)) moveYHash = Animator.StringToHash(moveYParam);
    }

    void OnEnable() => input.Enable();

    void OnDisable()
    {
        if (input.Player.SelectAttack != null)
            input.Player.SelectAttack.performed -= OnSelectAttackPerformed;
        if (spellRoutine != null) StopCoroutine(spellRoutine);
        spellRoutine = null;
        input.Disable();
    }

    void Start()
    {
        if (applyControllerDefaultsOnStart)
        {
            cc.height = standHeight;
            cc.center = standCenter;
        }
        else
        {
            standHeight = cc.height;
            standCenter = cc.center;
        }

        LockCursor(true);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // INPUTS (Cinemachine cuida da câmera; aqui só Move/Jump/etc.)
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        bool jumpPressed      = input.Player.Jump.WasPressedThisFrame();
        bool crouchHeld       = input.Player.Crouch.IsPressed();
        bool sneakHeld        = input.Player.SneakSlow.IsPressed();
        bool attackPressed    = input.Player.AttackB.WasPressedThisFrame();
        bool defendHeld       = input.Player.DefendB.IsPressed();
        bool interactPressed  = input.Player.Interact.WasPressedThisFrame();
        bool pausePressed     = input.Player.Pause.WasPressedThisFrame();
        bool mapPressed       = input.Player.Map.WasPressedThisFrame();
        bool inventoryPressed = (inventoryAction != null) && inventoryAction.WasPressedThisFrame();

        // Dispara trigger de pulo ANTES do movimento (para pegar o frame)
        if (jumpPressed && animator) animator.SetTrigger(jumpTriggerParam);

        HandleMove(dt, moveInput, ref jumpPressed, crouchHeld, sneakHeld);

        // --- ATTACK 1 = Purificação (spell) ---
        if (attackPressed && currentAttackSlot == 1 && Time.time >= nextSpellTime && spellRoutine == null)
        {
            spellRoutine = StartCoroutine(CastSpellRoutine());
        }

        // Logs opcionais
        #if UNITY_EDITOR
        if (attackPressed)   Debug.Log($"Attack pressed (slot {currentAttackSlot})");
        if (defendHeld)      { /* manter defesa ativa */ }
        if (interactPressed) Debug.Log("Interact");
        if (inventoryPressed){ Debug.Log("Inventory opened"); LockCursor(false); }
        if (pausePressed)    { Debug.Log("Pause"); LockCursor(false); }
        if (mapPressed)      Debug.Log("Map opened");
        #endif
    }

    IEnumerator CastSpellRoutine()
    {
        // Dispara a animação de cast
        if (animator) {
            animator.SetTrigger(spellTriggerParam);
        }

        // trava leve de movimento/jump no início do cast (sem mexer no Input System inteiro)
        if (castLockTime > 0f)
            moveLockUntil = Mathf.Max(moveLockUntil, Time.time + castLockTime);

        // Espera 1 frame para permitir a transição entrar no estado
        yield return null;

        // Descobre a duração do estado de spell atualmente tocando (robusto: Current e Next)
        float measuredLen = 0.3f; // fallback se não achar
        float timeout = 0.5f;     // meio segundo para localizar o estado
        int layer = spellLayerIndex;

        while (timeout > 0f)
        {
            if (animator.IsInTransition(layer))
            {
                var next = animator.GetNextAnimatorStateInfo(layer);
                if ((spellStateHash != -1 && next.shortNameHash == spellStateHash) ||
                    (spellStateHash == -1 && next.IsName(spellStateName)))
                {
                    measuredLen = next.length;
                    break;
                }
            }
            else
            {
                var cur = animator.GetCurrentAnimatorStateInfo(layer);
                if ((spellStateHash != -1 && cur.shortNameHash == spellStateHash) ||
                    (spellStateHash == -1 && cur.IsName(spellStateName)))
                {
                    measuredLen = cur.length;
                    break;
                }
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Define o cooldown usando a duração medida
        nextSpellTime = Time.time + measuredLen;

        // (Opcional) spawn de VFX no instante do cast (se preferir, use Animation Event no clip)
        if (spellVfxPrefab && spellMuzzle)
        {
            Instantiate(spellVfxPrefab, spellMuzzle.position, spellMuzzle.rotation);
        }

        // Aguarda a animação antes de liberar outro cast
        yield return new WaitForSeconds(measuredLen);

        // agora é seguro “limpar”
        if (animator) animator.ResetTrigger(spellTriggerParam);
        spellRoutine = null;
    }

    void HandleMove(float dt, Vector2 moveInput, ref bool jumpPressed, bool crouchHeld, bool sneakHeld)
    {
        // Gate de movimento/jump durante lock do cast
        if (Time.time < moveLockUntil)
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
        }

        // grounded ANTES de mover para validar pulo
        bool wasGrounded = cc.isGrounded;
        if (wasGrounded && velocity.y < 0f) velocity.y = -2f;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        // Direção no mundo baseada na CÂMERA (Cinemachine)
        Vector3 camFwd = playerCamera ? playerCamera.transform.forward : transform.forward;
        camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = playerCamera ? playerCamera.transform.right : transform.right;
        camRight.y = 0f; camRight.Normalize();
        Vector3 worldDir = (camFwd * inputDir.z + camRight * inputDir.x).normalized;

        // Reproduzir o crouch walk ao contrário quando andar para trás
        float crouchBackMult = 1f;
        if (crouchHeld && inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 flat = new Vector3(worldDir.x, 0f, worldDir.z).normalized;
            float fwdDot = Vector3.Dot(flat, transform.forward); // >0 frente, <0 costas
            if (fwdDot < -0.2f) crouchBackMult = -1f;            // margem pra não oscilar perto de 0
        }
        if (animator) animator.SetFloat("CrouchWalkSpeedMult", crouchBackMult);

        // velocidade alvo por estado
        float speed = walkSpeed;
        if (crouchHeld) speed = crouchSpeed;
        if (sneakHeld)  speed = Mathf.Min(speed, sneakSpeed);

        // no ar reduz velocidade horizontal
        if (!wasGrounded) speed *= airControl;

        Vector3 horizMove = worldDir * speed;

        // pulo (bloqueia se agachado)
        if (jumpPressed && wasGrounded && !crouchHeld)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // gravidade contínua
        velocity.y += gravity * dt;

        // Move: horizontal + vertical
        Vector3 displacement = (horizMove + Vector3.up * velocity.y) * dt;
        cc.Move(displacement);

        // grounded atualizado APÓS mover
        isGrounded = cc.isGrounded;

        // Ajuste do CC para agachar / levantar (estável em todos FPS)
        float targetH = crouchHeld ? crouchHeight : standHeight;
        float targetY = crouchHeld ? crouchCenter.y : standCenter.y;
        cc.height = Mathf.MoveTowards(cc.height, targetH, crouchLerp * dt);
        cc.center = new Vector3(cc.center.x, Mathf.MoveTowards(cc.center.y, targetY, crouchLerp * dt), cc.center.z);

        // Ruído simples
        noiseLevel = (sneakHeld || crouchHeld) ? 0f : (moveInput.sqrMagnitude > 0.01f ? 1f : 0f);

        // --- Animator parameters ---
        if (animator)
        {
            // Normaliza 0..1 pela velocidade de walk (magnitude)
            float speed01 = Mathf.InverseLerp(0f, Mathf.Max(0.01f, walkSpeed), horizMove.magnitude);

            // Sinal +frente / -costas com base na direção do input vs. forward do player
            float sign = 0f;
            Vector3 flatMove = new Vector3(worldDir.x, 0f, worldDir.z);
            if (flatMove.sqrMagnitude > 0.0001f)
            {
                flatMove.Normalize();
                float fwdDot = Vector3.Dot(flatMove, transform.forward); // >0 frente, <0 costas
                sign = Mathf.Sign(fwdDot);
            }

            // Em pé: usa sinal; agachado: usa valor absoluto para satisfazer as transições Speed>0
            float speedSigned   = speed01 * walkBlendPoint * sign;
            float speedForBlend = crouchHeld ? Mathf.Abs(speedSigned) : speedSigned;

            if (speedHash != -1) animator.SetFloat(speedHash, speedForBlend, animDampTime, Time.deltaTime);
            else                 animator.SetFloat(speedParam, speedForBlend, animDampTime, Time.deltaTime);

            if (groundedHash != -1) animator.SetBool(groundedHash, isGrounded);
            else if (!string.IsNullOrEmpty(groundedParam)) animator.SetBool(groundedParam, isGrounded);

            if (crouchHash != -1) animator.SetBool(crouchHash, crouchHeld);
            else if (!string.IsNullOrEmpty(crouchBoolParam)) animator.SetBool(crouchBoolParam, crouchHeld);

            // --- MoveX/MoveY para o Blend Tree 2D (Locomotion em pé) ---
            Vector3 localDir = transform.InverseTransformDirection(worldDir); // x=lateral, z=frente
            float mag01 = Mathf.InverseLerp(0f, Mathf.Max(0.01f, walkSpeed), horizMove.magnitude);
            float mx = localDir.x * walkBlendPoint * mag01;  // -0.5..+0.5
            float my = localDir.z * walkBlendPoint * mag01;  // -0.5..+0.5

            if (moveXHash != -1) animator.SetFloat(moveXHash, mx, animDampTime, Time.deltaTime);
            else                 animator.SetFloat(moveXParam, mx, animDampTime, Time.deltaTime);

            if (moveYHash != -1) animator.SetFloat(moveYHash, my, animDampTime, Time.deltaTime);
            else                 animator.SetFloat(moveYParam, my, animDampTime, Time.deltaTime);
        }

        // Alinha o corpo quando há movimento (TPS padrão)
        if (worldDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(worldDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 10f);
        }
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // Reset de câmera estilo TPS: faz o player olhar para frente da câmera (câmera fica atrás)
    void ResetCamera()
    {
        if (!playerCamera) return;
        Vector3 fwd = playerCamera.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(fwd);
    }

    // --- SelectAttack: identifica o binding específico que disparou ---
    void OnSelectAttackPerformed(InputAction.CallbackContext ctx)
    {
        var control = ctx.control;
        if (control == null) return;

        // Teclado (inclui numpad)
        if (control.device is Keyboard)
        {
            switch (control.name)
            {
                case "1": case "numpad1": currentAttackSlot = 1; break;
                case "2": case "numpad2": currentAttackSlot = 2; break;
                case "3": case "numpad3": currentAttackSlot = 3; break;
                case "4": case "numpad4": currentAttackSlot = 4; break;
                case "5": case "numpad5": currentAttackSlot = 5; break;
            }
            #if UNITY_EDITOR
            Debug.Log($"Attack slot set to {currentAttackSlot} (Keyboard: {control.name})");
            #endif
            return;
        }

        // Gamepad (D-Pad)
        if (control.device is Gamepad gp)
        {
            if      (control == gp.dpad.up)    currentAttackSlot = 1;
            else if (control == gp.dpad.down)  currentAttackSlot = 2;
            else if (control == gp.dpad.left)  currentAttackSlot = 3;
            else if (control == gp.dpad.right) currentAttackSlot = 4;

            #if UNITY_EDITOR
            Debug.Log($"Attack slot set to {currentAttackSlot} (Gamepad D-Pad: {control.name})");
            #endif
            return;
        }

        // Fallback
        #if UNITY_EDITOR
        Debug.Log($"SelectAttack (unmapped) from {control.path}");
        #endif
    }
}
