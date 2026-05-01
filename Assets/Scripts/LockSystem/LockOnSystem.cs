using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class LockOnSystem : MonoBehaviour
{
    #region Inspector - References

    [Header("Refs")]
    public Camera cam;

    [Tooltip("Transform que deve girar para encarar o alvo. Normalmente é o objeto raiz do Player.")]
    public Transform rotateRoot;

    #endregion

    #region Inspector - Detection

    [Header("Detection")]
    public float searchRadius = 20f;
    [Range(0f, 1f)] public float minDot = 0.2f;
    public LayerMask enemyMask = ~0;
    public LayerMask obstructionMask = 0;
    public bool drawDebug = false;

    #endregion

    #region Inspector - Target Switching

    [Header("Switch Target")]
    [SerializeField] private bool allowTargetSwitchWhileLocked = false;
    public float switchCooldown = 0.25f;
    public float lookSwitchDeadzone = 0.5f;

    #endregion

    #region Inspector - Camera Lock

    [Header("Camera Lock")]
    [Tooltip("Desativa o input da Cinemachine enquanto o lock-on estiver ativo.")]
    public bool lockCameraWhileLocked = true;

    [Tooltip("Procura automaticamente componentes de input da Cinemachine para desativar durante o lock-on.")]
    public bool autoFindCinemachineProviders = true;

    [Tooltip("Componentes desativados enquanto o lock-on estiver ativo. Normalmente: Cinemachine Input Axis Controller.")]
    public Behaviour[] disableWhileLocked;

    #endregion

    #region Inspector - Player Facing

    [Header("Player Facing While Locked")]
    public bool rotatePlayerTowardTargetWhileLocked = true;
    public float rotateSpeedDegPerSec = 540f;

    #endregion

    #region Runtime

    private PlayerInputActions input;
    private InputAction lockOnAction;
    private InputAction lookAction;

    public LockOnTarget current;
    private float nextSwitchTime = 0f;

    public bool IsLockedOn => current != null;
    public Transform CurrentAimPoint => current ? current.Pivot : null;

    #endregion

    #region Unity Events

    private void Reset()
    {
        if (!cam)
            cam = Camera.main;

        if (!rotateRoot)
            rotateRoot = transform;
    }

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;

        if (!rotateRoot)
            rotateRoot = transform;

        if (autoFindCinemachineProviders)
            FindCinemachineInputProviders();
    }

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance é NULO. O LockOnSystem não consegue pegar os inputs.");
            enabled = false;
            return;
        }

        input = InputManager.Instance.InputActions;
        lockOnAction = input.FindAction("LockOn", false);
        lookAction = input.FindAction("Look", false);
    }

    private void OnDisable()
    {
        ClearTarget();
        SetCameraInputLocked(false);
    }

    private void Update()
    {
        if (ShouldBlockLockOn())
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
            ClearTarget();
            return;
        }

        if (rotatePlayerTowardTargetWhileLocked)
            FaceTargetYawOnly(current.Pivot.position);

        if (allowTargetSwitchWhileLocked)
            HandleTargetSwitchInput();
    }

    #endregion

    #region Input

    // Bloqueia o lock-on quando menus ou telas de sistema estão abertas.
    private bool ShouldBlockLockOn()
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

    // Liga ou desliga o alvo travado.
    private void HandleLockToggleInput()
    {
        if (lockOnAction == null)
            return;

        if (!lockOnAction.WasPressedThisFrame())
            return;

        if (!IsLockedOn)
            AcquireTarget();
        else
            ClearTarget();
    }

    // Troca o alvo travado usando o eixo horizontal do Look, se habilitado.
    private void HandleTargetSwitchInput()
    {
        if (lookAction == null || Time.time < nextSwitchTime)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();
        float x = look.x;

        if (Mathf.Abs(x) < lookSwitchDeadzone)
            return;

        TrySwitchTarget(Mathf.Sign(x));
        nextSwitchTime = Time.time + switchCooldown;
    }

    #endregion

    #region Lock-On Core

    // Procura e trava no melhor alvo disponível.
    private void AcquireTarget()
    {
        LockOnTarget best = FindBestTarget();
        current = best;
        SetCameraInputLocked(current != null);
    }

    // Remove o alvo travado e libera o input da câmera.
    public void ClearTarget()
    {
        current = null;
        SetCameraInputLocked(false);
    }

    // Encontra o alvo com melhor pontuação dentro do campo de visão da câmera.
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

            Vector3 toTarget = (target.Pivot.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, toTarget);

            if (dot < minDot)
                continue;

            float distance = Vector3.Distance(GetSearchOrigin(), target.Pivot.position);
            float score = dot * 1.5f + (1f / Mathf.Max(1f, distance)) + target.priority * 0.25f;

            if (score > bestScore && HasLineOfSight(target.Pivot.position))
            {
                bestScore = score;
                best = target;
            }
        }

        return best;
    }

    // Troca para o alvo mais adequado à esquerda ou direita da câmera.
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

            Vector3 toTarget = (target.Pivot.position - cam.transform.position).normalized;
            float lateral = Vector3.Dot(camRight, toTarget);
            float facing = Mathf.Max(0f, Vector3.Dot(camForward, toTarget));

            if (Mathf.Sign(lateral) != Mathf.Sign(direction))
                continue;

            float score = Mathf.Abs(lateral) + facing;

            if (score > bestScore && HasLineOfSight(target.Pivot.position))
            {
                bestScore = score;
                candidate = target;
            }
        }

        if (candidate == null)
            return;

        current = candidate;
        SetCameraInputLocked(true);
    }

    #endregion

    #region Camera Lock

    // Procura componentes de input da Cinemachine para desativar durante o lock-on.
    private void FindCinemachineInputProviders()
    {
        Behaviour[] providers = Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b != null &&
                        (b.GetType().Name.Contains("CinemachineInput") ||
                         b.GetType().Name.Contains("InputAxisController")))
            .ToArray();

        if (providers.Length > 0)
            disableWhileLocked = providers;
    }

    // Trava ou libera apenas o input da câmera, sem mover o CameraTarget para o inimigo.
    private void SetCameraInputLocked(bool locked)
    {
        if (!lockCameraWhileLocked || disableWhileLocked == null)
            return;

        for (int i = 0; i < disableWhileLocked.Length; i++)
        {
            Behaviour provider = disableWhileLocked[i];

            if (provider == null)
                continue;

            provider.enabled = !locked;
        }
    }

    #endregion

    #region Player Facing

    // Gira o Player apenas no eixo Y para encarar o alvo travado.
    private void FaceTargetYawOnly(Vector3 targetPosition)
    {
        if (!rotateRoot)
            return;

        Vector3 direction = targetPosition - rotateRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        rotateRoot.rotation = Quaternion.RotateTowards(
            rotateRoot.rotation,
            targetRotation,
            rotateSpeedDegPerSec * Time.deltaTime
        );
    }

    #endregion

    #region Target Search

    // Coleta alvos dentro do raio de busca usando a layer de inimigos.
    private List<LockOnTarget> OverlapTargets()
    {
        List<LockOnTarget> results = new List<LockOnTarget>();
        Collider[] colliders = Physics.OverlapSphere(GetSearchOrigin(), searchRadius, enemyMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliders.Length; i++)
        {
            LockOnTarget target = colliders[i].GetComponentInParent<LockOnTarget>();

            if (target != null && !results.Contains(target))
                results.Add(target);
        }

        return results;
    }

    // Define a origem da busca de alvos.
    private Vector3 GetSearchOrigin()
    {
        if (rotateRoot != null)
            return rotateRoot.position;

        return transform.position;
    }

    // Verifica se o alvo ainda pode ser usado pelo lock-on.
    private bool TargetIsValid(LockOnTarget target)
    {
        if (!target)
            return false;

        if (!target.enabled)
            return false;

        if (!target.Pivot)
            return false;

        return true;
    }

    // Verifica se existe obstáculo entre a câmera e o alvo.
    private bool HasLineOfSight(Vector3 worldPosition)
    {
        if (!cam)
            return true;

        if (obstructionMask.value == 0)
            return true;

        Vector3 origin = cam.transform.position;
        Vector3 direction = worldPosition - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;
        return !Physics.Raycast(origin, direction, distance, obstructionMask, QueryTriggerInteraction.Ignore);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSearchOrigin(), searchRadius);

        if (current && current.Pivot)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(current.Pivot.position, 0.2f);
        }
    }

    #endregion
}
