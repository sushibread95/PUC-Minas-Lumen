using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class LockOnSystem : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                                   
    [Tooltip("Transform que deve girar para encarar o alvo...")]
    public Transform rotateRoot;

    [Header("Detection")]
    public float searchRadius = 20f;
    [Range(0f, 1f)] public float minDot = 0.2f;
    public LayerMask enemyMask = ~0;
    public LayerMask obstructionMask = 0;
    public bool drawDebug = false;

    [Header("Switch Target (Right Stick)")]
    public float switchCooldown = 0.25f;
    public float lookSwitchDeadzone = 0.5f;

    [Header("Camera Lock")]
    public bool lockCameraWhileLocked = true;
    public bool autoFindCinemachineProviders = true;
    public Behaviour[] disableWhileLocked;

    [Header("Player Facing While Locked")]
    public bool rotatePlayerTowardTargetWhileLocked = true;
    public float rotateSpeedDegPerSec = 540f;


    // runtime (Input)
    private PlayerInputActions input; 
    private InputAction lockOnAction;                
    private InputAction lookAction;                  

    // runtime (state)
    public LockOnTarget current;
    private float nextSwitchTime = 0f;

    // API pública
    public bool IsLockedOn => current != null;
    public Transform CurrentAimPoint => current ? current.Pivot : null;

    void Reset()
    {
        if (!cam) cam = Camera.main;
    }

    // Awake() é só para pegar refs internas
    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!rotateRoot) rotateRoot = transform; 

        // Auto-detecta componentes da câmera para desabilitar
        if (autoFindCinemachineProviders)
        {
             var providers = Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None)
                .Where(b => b != null && (b.GetType().Name.Contains("CinemachineInput") || b.GetType().Name.Contains("InputAxisController"))) 
                .ToArray();
            if (providers.Length > 0) disableWhileLocked = providers;
        }
    }

    // A lógica de Input vai para o Start()
    void Start()
    {
        if (InputManager.Instance == null)
        {
             Debug.LogError("InputManager.Instance é NULO. O LockOnSystem não consegue pegar os inputs.");
             this.enabled = false;
             return; 
        }
        input = InputManager.Instance.InputActions;
        
        lockOnAction = input.FindAction("LockOn", false);
        lookAction   = input.FindAction("Look",   false);
    }

    void OnDisable()
    {
        // Garante que a câmera seja destravada se o objeto for desabilitado
        if (IsLockedOn) SetCameraLock(false);
    }

    void Update()
    {
        // --- CLÁUSULA DE GUARDA MESTRA (INTEGRADA) ---
        // Se o input não existir, OU o Pause estiver aberto, OU o Inventário estiver aberto...
        if (input == null || 
           (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) ||
           (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
        {
            if (IsLockedOn) ClearTarget(); // Desliga o lock se pausar
            return; 
        }
        // ---------------------------------------------
        
        // Lógica original de Lock-On
        if (lockOnAction != null && lockOnAction.WasPressedThisFrame()) 
        {
            if (!IsLockedOn) AcquireTarget();
            else ClearTarget();
        }
        
        if (IsLockedOn)
        {
            if (!TargetIsValid(current))
            {
                ClearTarget();
                return;
            }
            if (rotatePlayerTowardTargetWhileLocked)
            {
                FaceTargetYawOnly(current.Pivot.position);
            }
            if (lookAction != null && Time.time >= nextSwitchTime)
            {
                Vector2 look = lookAction.ReadValue<Vector2>();
                float x = look.x;
                if (Mathf.Abs(x) >= lookSwitchDeadzone)
                {
                    TrySwitchTarget(Mathf.Sign(x));
                    nextSwitchTime = Time.time + switchCooldown;
                }
            }
        }
    }
    
    // --- Funções de Lógica de Lock-On (sem modificações) ---
    
    void AcquireTarget()
    {
        LockOnTarget best = FindBestTarget();
        current = best;
        SetCameraLock(current != null);
    }

    void ClearTarget()
    {
        current = null;
        SetCameraLock(false);
    }

    void SetCameraLock(bool locked)
    {
        if (!lockCameraWhileLocked) return;
        if (disableWhileLocked != null)
        {
            for (int i = 0; i < disableWhileLocked.Length; i++)
            {
                var b = disableWhileLocked[i];
                if (!b) continue;
                b.enabled = !locked;
            }
        }
    }

    LockOnTarget FindBestTarget()
    {
        var list = OverlapTargets();
        if (list.Count == 0) return null;
        float bestScore = float.NegativeInfinity;
        LockOnTarget best = null;
        for (int i = 0; i < list.Count; i++)
        {
            var tgt = list[i];
            if (!TargetIsValid(tgt)) continue;
            Vector3 to = (tgt.Pivot.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, to);
            if (dot < minDot) continue;
            float dist  = Vector3.Distance(rotateRoot.position, tgt.Pivot.position);
            float score = dot * 1.5f + (1f / Mathf.Max(1f, dist)) + tgt.priority * 0.25f;
            if (score > bestScore && HasLineOfSight(tgt.Pivot.position))
            {
                bestScore = score;
                best = tgt;
            }
        }
        return best;
    }

     void TrySwitchTarget(float direction)
    {
        var list = OverlapTargets();
        if (list.Count == 0 || !cam) return;
        LockOnTarget candidate = null;
        float best = float.NegativeInfinity;
        Vector3 camRight = cam.transform.right;
        Vector3 camForward = cam.transform.forward;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == current || !TargetIsValid(t)) continue;
            Vector3 to = (t.Pivot.position - cam.transform.position).normalized;
            float lateral = Vector3.Dot(camRight, to); 
            float facing  = Mathf.Max(0f, Vector3.Dot(camForward, to)); 
            if (Mathf.Sign(lateral) != Mathf.Sign(direction)) continue;
            float score = Mathf.Abs(lateral) + facing; 
            if (score > best && HasLineOfSight(t.Pivot.position))
            {
                best = score;
                candidate = t;
            }
        }
        if (candidate != null)
        {
            current = candidate;
            SetCameraLock(true);
        }
    }

    void FaceTargetYawOnly(Vector3 targetPos)
    {
        if (!rotateRoot) return;
        Vector3 from = rotateRoot.position;
        Vector3 to = targetPos;
        Vector3 dir = to - from;
        dir.y = 0f; 
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        rotateRoot.rotation = Quaternion.RotateTowards(rotateRoot.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
    }

    List<LockOnTarget> OverlapTargets()
    {
        var results = new List<LockOnTarget>();
        Collider[] cols = Physics.OverlapSphere(transform.position, searchRadius, enemyMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < cols.Length; i++)
        {
            var t = cols[i].GetComponentInParent<LockOnTarget>();
            if (t != null && !results.Contains(t)) results.Add(t);
        }
        return results;
    }

    bool TargetIsValid(LockOnTarget t)
    {
        if (!t) return false;
        if (!t.Pivot) return false;
        return true;
    }

     bool HasLineOfSight(Vector3 worldPos)
    {
        if (!cam) return true;
        Vector3 origin = cam.transform.position;
        Vector3 dir = (worldPos - origin).normalized;
        float dist = Vector3.Distance(origin, worldPos);
        if (dist <= 0.001f) return true;
        if (obstructionMask.value == 0) return true; 
        return !Physics.Raycast(origin, dir, dist, obstructionMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
        if (current)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(current.Pivot.position, 0.2f);
        }
    }
}