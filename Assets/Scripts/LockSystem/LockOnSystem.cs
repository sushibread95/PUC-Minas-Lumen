using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

// Não precisa mais do RequireComponent
public class LockOnSystem : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                                   
    // public PlayerControllerSystem player; // <-- Removido, não precisamos mais
    // public PlayerInput playerInput;      // <-- Removido, pegamos do InputManager
    [Tooltip("Transform que deve girar para encarar o alvo...")]
    public Transform rotateRoot;

    // ... (O resto dos seus [Header] continua igual) ...
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
    // private PlayerInputActions inputAssetFallback; // <-- REMOVIDO
    private PlayerInputActions input; // <-- AGORA VEM DO MANAGER
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
        // if (!player) player = GetComponent<PlayerControllerSystem>(); // <-- Removido
    }

    // --- MUDANÇA (INÍCIO) ---
    // Awake() é só para pegar refs internas
    void Awake()
    {
        if (!cam) cam = Camera.main;
        // O rotateRoot agora pega 'transform' se o player não existir mais como ref
        if (!rotateRoot) rotateRoot = transform; 

        // Popular automaticamente provedores de input de câmera (isso estava certo)
        if (autoFindCinemachineProviders)
        {
            // ... (seu código de autoFind continua igual) ...
             var providers = Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b != null && b.GetType().Name.Contains("CinemachineInput")) 
                .ToArray();
            if (providers.Length > 0) disableWhileLocked = providers;
        }
    }

    // A lógica de Input vai para o Start()
    void Start()
    {
        // Pega o Input do Manager central
        if (InputManager.Instance == null)
        {
             Debug.LogError("InputManager.Instance é NULO. O LockOnSystem não consegue pegar os inputs.");
             return; // Sai se o manager não existir
        }
        input = InputManager.Instance.InputActions;

        // Pega as ações (agora do 'input' que veio do manager)
        lockOnAction = input.FindAction("LockOn", false);
        lookAction   = input.FindAction("Look",   false);

        // Não precisamos mais do Enable() aqui, o InputManager controla isso.
    }
    // --- MUDANÇA (FIM) ---

    void OnDisable()
    {
        if (IsLockedOn) SetCameraLock(false);
    }

    void Update()
    {
        if (input == null) return;
        // --- MUDANÇA (FIM) ---

        // Sua cláusula de guarda do Pause já estava aqui e correta
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            if (IsLockedOn) ClearTarget();
            return;
        }

        // alterna lock on/off (usa 'lockOnAction' que pegamos no Start)
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

            // Troca de alvo (usa 'lookAction' que pegamos no Start)
            if (lookAction != null && Time.time >= nextSwitchTime)
            {
                Vector2 look = lookAction.ReadValue<Vector2>();
                // ... (resto da lógica de switch continua igual) ...
                float x = look.x;
                if (Mathf.Abs(x) >= lookSwitchDeadzone)
                {
                    TrySwitchTarget(Mathf.Sign(x));
                    nextSwitchTime = Time.time + switchCooldown;
                }
            }
        }
    }
    
    
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

            // filtra por lado
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
        dir.y = 0f; // yaw only
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        rotateRoot.rotation = Quaternion.RotateTowards(rotateRoot.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
    }

    // Utilidades
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
        if (obstructionMask.value == 0) return true; // sem máscara => ignore LOS
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