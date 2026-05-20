using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class LockOnSystem : MonoBehaviour
{
    #region Inspector - Referências

    [Header("REFERÊNCIAS PRINCIPAIS")]
    [Tooltip("Câmera principal da cena. Use a MainCamera que possui o CinemachineBrain.")]
    [SerializeField] private Camera cam;

    [Tooltip("Objeto raiz que deve virar para o alvo. Normalmente é o Player.")]
    [SerializeField] private Transform rotateRoot;

    [Tooltip("Cinemachine Orbital Follow da câmera de gameplay.")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    [Tooltip("Cinemachine Input Axis Controller da câmera. Ele é desligado somente durante o lock-on.")]
    [SerializeField] private CinemachineInputAxisController cameraInputAxisController;

    #endregion

    #region Inspector - Detecção

    [Header("DETECÇÃO DE ALVO")]
    [SerializeField] private float searchRadius = 20f;

    [Tooltip("Quanto o alvo precisa estar próximo do centro/frente da câmera para ser aceito.")]
    [Range(0f, 1f)]
    [SerializeField] private float minScreenDot = 0.15f;

    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private LayerMask obstructionMask = 0;

    [Header("LIMITES DO LOCK")]
    [SerializeField] private bool clearLockWhenTooFar = true;
    [SerializeField] private float maxLockDistance = 18f;
    [SerializeField] private bool clearLockWhenTargetDiesOrDisables = true;

    #endregion

    #region Inspector - Input do Lock

    [Header("INPUT DO LOCK")]
    [Tooltip("Evita duplo toggle quando mais de um sistema de input tenta ler a mesma tecla no mesmo instante.")]
    [SerializeField] private float lockToggleCooldown = 0.18f;

    #endregion

    #region Inspector - Câmera Durante Lock

    [Header("CÂMERA DURANTE LOCK")]
    [Tooltip("Desliga o input manual da câmera enquanto estiver travado no alvo.")]
    [SerializeField] private bool disableCameraInputWhileLocked = true;

    [Tooltip("Garante todo frame que o mouse não volte a controlar a câmera durante o lock.")]
    [SerializeField] private bool enforceInputDisableEveryFrame = true;

    [Tooltip("Controla diretamente o eixo horizontal do Orbital Follow durante o lock-on.")]
    [SerializeField] private bool driveOrbitalHorizontalAxis = true;

    [Tooltip("Use 180 para a câmera ficar atrás do player olhando para o inimigo. Se ficar invertido no seu rig, teste 0.")]
    [SerializeField] private float lockOrbitYawOffset = 180f;

    [Tooltip("Velocidade com que a câmera alinha a órbita ao alvo durante o lock-on.")]
    [SerializeField] private float lockOrbitYawSpeed = 540f;

    [Tooltip("Mantém a altura vertical da câmera congelada ao entrar no lock-on.")]
    [SerializeField] private bool freezeVerticalAxisWhileLocked = true;

    [Tooltip("Reduz a força de correção da câmera quando o player está muito perto do alvo.")]
    [SerializeField] private float closeRangeSoftDistance = 1.35f;

    [Range(0.1f, 1f)]
    [SerializeField] private float closeRangeYawMultiplier = 0.45f;

    #endregion

    #region Inspector - Player Durante Lock

    [Header("PLAYER DURANTE LOCK")]
    [SerializeField] private bool rotatePlayerTowardTargetWhileLocked = true;
    [SerializeField] private float rotateSpeedDegPerSec = 540f;

    #endregion

    #region Inspector - Troca de Alvo

    [Header("TROCA DE ALVO")]
    [Tooltip("Deixe desligado se o mouse estiver causando troca acidental de alvo.")]
    [SerializeField] private bool allowTargetSwitchWhileLocked = false;

    [SerializeField] private float switchCooldown = 0.25f;
    [SerializeField] private float lookSwitchDeadzone = 0.65f;

    #endregion

    #region Inspector - Debug

    [Header("DEBUG")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField, HideInInspector] private bool logCameraSetup = false;

    #endregion

    #region Runtime

    private PlayerInputActions input;
    private InputAction lockOnAction;
    private InputAction lookAction;

    private bool cachedCameraInputState;
    private bool hasCachedCameraInputState;
    private bool hasStoredAxes;
    private float storedVerticalAxis;

    private float nextSwitchTime;
    private float nextLockToggleTime;

    [HideInInspector] public LockOnTarget current;

    public bool IsLockedOn => current != null;
    public Transform CurrentAimPoint => current != null ? current.AimPoint : null;

    #endregion

    #region Unity Lifecycle

    private void Reset()
    {
        cam = Camera.main;
        rotateRoot = transform;
        AutoFindCameraComponents();
    }

    private void Awake()
    {
        CacheBasicReferences();
        AutoFindCameraComponents();
    }

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError($"[{nameof(LockOnSystem)}] InputManager.Instance é nulo. Lock-on desativado.");
            enabled = false;
            return;
        }

        input = InputManager.Instance.InputActions;
        lockOnAction = input.FindAction("LockOn", false);
        lookAction = input.FindAction("Look", false);

        if (lockOnAction == null)
            Debug.LogWarning($"[{nameof(LockOnSystem)}] A action 'LockOn' não foi encontrada no InputActions.");
    }

    private void Update()
    {
        if (ShouldCancelLockState())
        {
            if (IsLockedOn)
                ClearTarget();

            return;
        }

        HandleLockToggleInput();

        if (!IsLockedOn)
            return;

        if (!TargetIsValid(current))
        {
            if (clearLockWhenTargetDiesOrDisables)
                ClearTarget();

            return;
        }

        if (clearLockWhenTooFar && rotateRoot != null && CurrentAimPoint != null)
        {
            float distance = Vector3.Distance(rotateRoot.position, CurrentAimPoint.position);
            if (distance > maxLockDistance)
            {
                ClearTarget();
                return;
            }
        }

        if (rotatePlayerTowardTargetWhileLocked)
            FaceTargetYawOnly(CurrentAimPoint.position);

        if (allowTargetSwitchWhileLocked)
            HandleTargetSwitchInput();
    }

    private void LateUpdate()
    {
        if (!IsLockedOn)
            return;

        if (disableCameraInputWhileLocked && enforceInputDisableEveryFrame)
            SetCameraInputEnabled(false);

        if (driveOrbitalHorizontalAxis)
            DriveOrbitalAxisDirectly();
    }

    private void OnDisable()
    {
        ClearTarget();
    }

    private void OnDestroy()
    {
        RestoreCameraInput();
    }

    #endregion

    #region Setup

    // Busca referências básicas sem depender da cena de boot.
    private void CacheBasicReferences()
    {
        if (cam == null)
            cam = Camera.main;

        if (rotateRoot == null)
            rotateRoot = transform;
    }

    // Encontra componentes da câmera se não foram configurados manualmente.
    private void AutoFindCameraComponents()
    {
        if (orbitalFollow == null)
            orbitalFollow = FindFirstObjectByType<CinemachineOrbitalFollow>();

        if (cameraInputAxisController == null)
            cameraInputAxisController = FindFirstObjectByType<CinemachineInputAxisController>();

        if (logCameraSetup)
            Debug.Log($"[{nameof(LockOnSystem)}] OrbitalFollow: {orbitalFollow}, InputAxis: {cameraInputAxisController}");
    }

    #endregion

    #region Lock State

    // Lê o input de lock com proteção contra duplo toggle.
    private void HandleLockToggleInput()
    {
        if (lockOnAction == null)
            return;

        if (!lockOnAction.WasPressedThisFrame())
            return;

        if (Time.unscaledTime < nextLockToggleTime)
            return;

        nextLockToggleTime = Time.unscaledTime + lockToggleCooldown;

        if (IsLockedOn)
            ClearTarget();
        else
            AcquireTarget();
    }

    // Procura e trava no melhor alvo disponível.
    private void AcquireTarget()
    {
        CacheBasicReferences();
        AutoFindCameraComponents();

        LockOnTarget best = FindBestTarget();
        if (best == null)
            return;

        current = best;
        StoreCameraStateForLock();
        SetCameraInputEnabled(false);
    }

    // Remove o alvo atual e devolve o controle manual da câmera.
    public void ClearTarget()
    {
        current = null;
        hasStoredAxes = false;
        RestoreCameraInput();
    }

    // Salva o estado da câmera em TODA entrada no lock-on.
    private void StoreCameraStateForLock()
    {
        if (cameraInputAxisController == null)
            AutoFindCameraComponents();

        if (cameraInputAxisController != null)
        {
            cachedCameraInputState = cameraInputAxisController.enabled;
            hasCachedCameraInputState = true;
        }

        if (orbitalFollow != null)
        {
            storedVerticalAxis = orbitalFollow.VerticalAxis.Value;
            hasStoredAxes = true;
        }
    }

    // Desliga apenas o input da câmera. Não altera InputManager nem PlayerInput.
    private void SetCameraInputEnabled(bool enabledState)
    {
        if (!disableCameraInputWhileLocked)
            return;

        if (cameraInputAxisController == null)
            AutoFindCameraComponents();

        if (cameraInputAxisController != null)
            cameraInputAxisController.enabled = enabledState;
    }

    // Restaura o controle manual da câmera. Se não houver cache, volta ligado por segurança.
    private void RestoreCameraInput()
    {
        if (cameraInputAxisController != null)
            cameraInputAxisController.enabled = hasCachedCameraInputState ? cachedCameraInputState : true;

        hasCachedCameraInputState = false;
    }

    // Estados em que o lock-on não deve continuar ativo.
    private bool ShouldCancelLockState()
    {
        if (input == null)
            return true;

        if (DeathScreenManager.Instance != null && DeathScreenManager.Instance.IsDeathScreenActive)
            return true;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
            return true;

        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
            return true;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return true;

        return false;
    }

    #endregion

    #region Target Detection

    // Escolhe o alvo com melhor posição na tela, distância e prioridade.
    private LockOnTarget FindBestTarget()
    {
        List<LockOnTarget> targets = OverlapTargets();
        if (targets.Count == 0 || cam == null)
            return null;

        float bestScore = float.NegativeInfinity;
        LockOnTarget best = null;

        for (int i = 0; i < targets.Count; i++)
        {
            LockOnTarget target = targets[i];
            if (!TargetIsValid(target))
                continue;

            Vector3 toTargetFromCamera = (target.AimPoint.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, toTargetFromCamera);
            if (dot < minScreenDot)
                continue;

            float distance = rotateRoot != null
                ? Vector3.Distance(rotateRoot.position, target.AimPoint.position)
                : Vector3.Distance(transform.position, target.AimPoint.position);

            Vector3 viewport = cam.WorldToViewportPoint(target.AimPoint.position);
            float centerError = Mathf.Abs(viewport.x - 0.5f) + Mathf.Abs(viewport.y - 0.5f);

            float score = dot * 2f - centerError + (1f / Mathf.Max(1f, distance)) + target.priority * 0.25f;

            if (score > bestScore && HasLineOfSight(target.AimPoint.position))
            {
                bestScore = score;
                best = target;
            }
        }

        return best;
    }

    // Coleta alvos próximos usando colliders do inimigo.
    private List<LockOnTarget> OverlapTargets()
    {
        List<LockOnTarget> results = new List<LockOnTarget>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius, enemyMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliders.Length; i++)
        {
            LockOnTarget target = colliders[i].GetComponentInParent<LockOnTarget>();
            if (target != null && !results.Contains(target))
                results.Add(target);
        }

        return results;
    }

    private bool TargetIsValid(LockOnTarget target)
    {
        if (target == null)
            return false;

        if (!target.IsTargetable)
            return false;

        if (target.AimPoint == null)
            return false;

        return target.gameObject.activeInHierarchy;
    }

    private bool HasLineOfSight(Vector3 worldPos)
    {
        if (cam == null || obstructionMask.value == 0)
            return true;

        Vector3 origin = cam.transform.position;
        Vector3 direction = worldPos - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        return !Physics.Raycast(origin, direction.normalized, distance, obstructionMask, QueryTriggerInteraction.Ignore);
    }

    #endregion

    #region Target Switch

    // Troca alvo pelo eixo horizontal, caso a opção esteja ativa.
    private void HandleTargetSwitchInput()
    {
        if (lookAction == null || Time.time < nextSwitchTime)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();
        if (Mathf.Abs(look.x) < lookSwitchDeadzone)
            return;

        TrySwitchTarget(Mathf.Sign(look.x));
        nextSwitchTime = Time.time + switchCooldown;
    }

    private void TrySwitchTarget(float direction)
    {
        List<LockOnTarget> targets = OverlapTargets();
        if (targets.Count == 0 || cam == null)
            return;

        LockOnTarget candidate = null;
        float bestScore = float.NegativeInfinity;
        Vector3 camRight = cam.transform.right;
        Vector3 camForward = cam.transform.forward;

        for (int i = 0; i < targets.Count; i++)
        {
            LockOnTarget target = targets[i];
            if (target == current || !TargetIsValid(target))
                continue;

            Vector3 toTarget = (target.AimPoint.position - cam.transform.position).normalized;
            float lateral = Vector3.Dot(camRight, toTarget);
            float facing = Mathf.Max(0f, Vector3.Dot(camForward, toTarget));

            if (Mathf.Sign(lateral) != Mathf.Sign(direction))
                continue;

            float score = Mathf.Abs(lateral) + facing + target.priority * 0.1f;
            if (score > bestScore && HasLineOfSight(target.AimPoint.position))
            {
                bestScore = score;
                candidate = target;
            }
        }

        if (candidate != null)
            current = candidate;
    }

    #endregion

    #region Camera Driver

    // Define a órbita horizontal pela direção Player -> Inimigo.
    private void DriveOrbitalAxisDirectly()
    {
        if (orbitalFollow == null || rotateRoot == null || CurrentAimPoint == null)
            return;

        Vector3 toTarget = CurrentAimPoint.position - rotateRoot.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
            return;

        float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg + lockOrbitYawOffset;
        float currentYaw = orbitalFollow.HorizontalAxis.Value;

        float distance = toTarget.magnitude;
        float speed = lockOrbitYawSpeed;

        if (distance <= closeRangeSoftDistance)
            speed *= closeRangeYawMultiplier;

        orbitalFollow.HorizontalAxis.Value = Mathf.MoveTowardsAngle(currentYaw, desiredYaw, speed * Time.deltaTime);

        if (freezeVerticalAxisWhileLocked && hasStoredAxes)
            orbitalFollow.VerticalAxis.Value = storedVerticalAxis;
    }

    #endregion

    #region Player Facing

    // Gira apenas o eixo Y do player para encarar o AimPoint.
    private void FaceTargetYawOnly(Vector3 targetPosition)
    {
        if (rotateRoot == null)
            return;

        Vector3 direction = targetPosition - rotateRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        rotateRoot.rotation = Quaternion.RotateTowards(
            rotateRoot.rotation,
            targetRotation,
            rotateSpeedDegPerSec * Time.deltaTime
        );
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        if (current != null && current.AimPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(current.AimPoint.position, 0.18f);
            Gizmos.DrawLine(transform.position, current.AimPoint.position);
        }
    }

    #endregion
}
