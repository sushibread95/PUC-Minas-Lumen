using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PlayerControllerSystem))]
public class LockOnSystem : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                                   // Main Camera (com CinemachineBrain)
    public PlayerControllerSystem player;                // Controller do Player
    public PlayerInput playerInput;                      // PlayerInput do Player
    [Tooltip("Transform que deve girar para encarar o alvo enquanto lockado (normalmente o root do Player). Se vazio, usa player.transform.")]
    public Transform rotateRoot;

    [Header("Detection")]
    public float searchRadius = 20f;
    [Range(0f, 1f)] public float minDot = 0.2f;
    public LayerMask enemyMask = ~0;
    public LayerMask obstructionMask = 0;
    public bool drawDebug = false;

    [Header("Switch Target (Right Stick)")]
    [Tooltip("Tempo mínimo entre trocas de alvo.")]
    public float switchCooldown = 0.25f;

    [Tooltip("Deadzone horizontal do LOOK para trocar de alvo (R Stick X).")]
    public float lookSwitchDeadzone = 0.5f;

    [Header("Camera Lock")]
    [Tooltip("Se true, desabilita provedores de input da câmera (ex.: CinemachineInputProvider) enquanto estiver lockado. A ação 'Look' permanece ativa para lermos o Right Stick e trocar alvos.")]
    public bool lockCameraWhileLocked = true;

    [Tooltip("Se marcado, encontra automaticamente componentes Cinemachine de input para desabilitar durante o lock (em toda a cena).")]
    public bool autoFindCinemachineProviders = true;

    [Tooltip("Componentes extras a desabilitar durante o lock (ex.: CinemachineInputProvider). Pode ser preenchido automaticamente.")]
    public Behaviour[] disableWhileLocked;

    [Header("Player Facing While Locked")]
    [Tooltip("Se true, o Player gira automaticamente (yaw) para encarar o alvo durante o lock.")]
    public bool rotatePlayerTowardTargetWhileLocked = true;

    [Tooltip("Velocidade de rotação ao encarar o alvo (graus/seg).")] 
    public float rotateSpeedDegPerSec = 540f;

    // runtime (Input)
    private PlayerInputActions inputAssetFallback;   // fallback caso não haja PlayerInput
    private InputAction lockOnAction;                // Botão para ligar/desligar lock (ex.: R3 click)
    private InputAction lookAction;                  // Vector2 (Right Stick) — usamos o X para trocar alvo

    // runtime (state)
    public LockOnTarget current;
    private float nextSwitchTime = 0f;

    // API pública usada por outros sistemas
    public bool IsLockedOn => current != null;
    public Transform CurrentAimPoint => current ? current.Pivot : null;

    void Reset()
    {
        if (!cam) cam = Camera.main;
        if (!player) player = GetComponent<PlayerControllerSystem>();
    }

    void Awake()
    {
        if (!player) player = GetComponent<PlayerControllerSystem>();
        if (!cam) cam = Camera.main;
        if (!rotateRoot) rotateRoot = player ? player.transform : transform;

        // Pega ações a partir do PlayerInput (recomendado) ou do asset gerado (fallback)
        if (playerInput && playerInput.actions != null)
        {
            lockOnAction = playerInput.actions.FindAction("LockOn", false);
            lookAction   = playerInput.actions.FindAction("Look",   false);
        }
        if (lockOnAction == null || lookAction == null)
        {
            // Fallback seguro — apenas se a classe gerada existir no projeto
            try
            {
                inputAssetFallback = new PlayerInputActions();
                if (lockOnAction == null) lockOnAction = inputAssetFallback.FindAction("LockOn", false);
                if (lookAction    == null) lookAction    = inputAssetFallback.FindAction("Look",   false);
            }
            catch { /* se não existir, seguimos apenas com as que acharmos */ }
        }

        if (lockOnAction != null && !lockOnAction.enabled) lockOnAction.Enable();
        if (lookAction    != null && !lookAction.enabled)    lookAction.Enable();

        // Popular automaticamente provedores de input de câmera
        if (autoFindCinemachineProviders)
        {
            var providers = Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b != null && b.GetType().Name.Contains("CinemachineInput")) // cobre CinemachineInputProvider e variações
                .ToArray();
            if (providers.Length > 0) disableWhileLocked = providers;
        }
    }

    void OnDisable()
    {
        // segurança: se for desabilitado ainda em lock, devolve o controle
        if (IsLockedOn) SetCameraLock(false);
    }

    void Update()
    {
        // alterna lock on/off
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

            // 1) Faz o player encarar o alvo (yaw only)
            if (rotatePlayerTowardTargetWhileLocked)
            {
                FaceTargetYawOnly(current.Pivot.position);
            }

            // 2) Troca de alvo pelo eixo X do LOOK (Right Stick) — L3 livre para movimento
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

    // ===== Core =====
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

        // Desabilita provedores de input da CÂMERA (ex.: CinemachineInputProvider),
        // mas mantém a ação LOOK ativa para lermos o Right Stick e trocar alvos.
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

    // Seleção
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

        // escolhe o vizinho mais à esquerda/direita do alvo atual, na tela
        LockOnTarget candidate = null;
        float best = float.NegativeInfinity;

        Vector3 camRight = cam.transform.right;
        Vector3 camForward = cam.transform.forward;

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == current || !TargetIsValid(t)) continue;

            Vector3 to = (t.Pivot.position - cam.transform.position).normalized;
            float lateral = Vector3.Dot(camRight, to); // < 0 = esquerda, > 0 = direita
            float facing  = Mathf.Max(0f, Vector3.Dot(camForward, to)); // quanto está à frente da câmera

            // filtra por lado
            if (Mathf.Sign(lateral) != Mathf.Sign(direction)) continue;

            float score = Mathf.Abs(lateral) + facing; // prioriza lateral + estar à frente
            if (score > best && HasLineOfSight(t.Pivot.position))
            {
                best = score;
                candidate = t;
            }
        }

        if (candidate != null)
        {
            current = candidate;
            // já estamos lockados, apenas mantém a câmera travada
            SetCameraLock(true);
        }
    }

    // === Rotação do Player para o alvo (apenas yaw) ===
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
