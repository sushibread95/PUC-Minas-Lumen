using UnityEngine;

[System.Serializable]
public struct Spell
{
    [Header("Components")]
    public Transform[] cannonBarrels;
    public GameObject projectile;
    public GameObject muzzleParticles;
    public GameObject dudMuzzleParticles;

    [Header("Casting Animation Parameters")]
    public string spellTriggerParam;
    public string spellStateName;
    public int    spellLayerIndex;
    public float  castLockTime;

    [Header("Casting Atributes")]
    public float fireRate;
    public int   magazine;
    public float cooldown;

    [Header("Mana Atributes")]
    public Effect[] castEffects;

    [Header("Barrel Atributes")]
    public int   projectilePerShot;
    public float spreadAngle;
    public bool  fixedSpread;
    public float recoilForce;
}

public class Cannon : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthSystem health;

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 25f;

    [Header("Lock-On Aim")]
    [Tooltip("Arraste o LockOnSystem do Player. Se houver alvo lockado, o disparo usa a posição atual do alvo como direção inicial.")]
    public LockOnSystem lockOn;
    [Tooltip("Se true, usa a mira do Lock-On quando disponível; caso contrário usa o forward do barrel.")]
    public bool useLockAimWhenAvailable = true;
    [Tooltip("Se true, zera o spread quando estiver lockado (útil para teste de acerto).")]
    public bool ignoreSpreadWhenLocked = false;

    [Header("Debug")]
    public bool debugDrawRay = false;
    public Color debugRayColor = new Color(1, 0.6f, 0.1f, 1);

    public void Fire(Spell spellToCast)
    {
        // --- Guards / validações ---
        if (spellToCast.cannonBarrels == null || spellToCast.cannonBarrels.Length == 0)
        {
            Debug.LogWarning("Cannon.Fire: nenhum barrel configurado.");
            return;
        }
        if (spellToCast.projectile == null)
        {
            Debug.LogWarning("Cannon.Fire: projectile não atribuído.");
            return;
        }

        // Checa custo/efeitos UMA vez por volley de disparo
        bool canCast = (health == null) || health.ApplyEffect(spellToCast.castEffects);

        for (int j = 0; j < spellToCast.cannonBarrels.Length; j++)
        {
            Transform barrel = spellToCast.cannonBarrels[j];
            if (!barrel) continue;

            if (canCast)
            {
                if (spellToCast.muzzleParticles)
                    ObjectPoolingSystem.SpawnObject(spellToCast.muzzleParticles, barrel.position, barrel.rotation);

                // === DIREÇÃO BASE ===
                Vector3 aimPos;
                bool hasLockAim = useLockAimWhenAvailable && lockOn && lockOn.IsLockedOn && lockOn.CurrentAimPoint;

                if (hasLockAim)
                {
                    aimPos = lockOn.CurrentAimPoint.position;
                }
                else
                {
                    // mira “reto” — finge um ponto à frente para construir a direção
                    aimPos = barrel.position + barrel.forward * 1000f;
                }

                Vector3 baseDir = (aimPos - barrel.position);
                if (baseDir.sqrMagnitude < 0.0001f) baseDir = barrel.forward;
                baseDir.Normalize();
                Quaternion baseRot = Quaternion.LookRotation(baseDir, Vector3.up);

                if (debugDrawRay)
                    Debug.DrawLine(barrel.position, barrel.position + baseDir * 30f, debugRayColor, 1.0f);

                // === DISPARO ===
                int shots = Mathf.Max(1, spellToCast.projectilePerShot);

                // trava spread se estiver lockado (opcional, para teste de precisão)
                bool applySpread = !(ignoreSpreadWhenLocked && hasLockAim);
                float spread = applySpread ? Mathf.Max(0f, spellToCast.spreadAngle) : 0f;

                if (shots == 1 || spread <= 0f)
                {
                    SpawnAndPush(spellToCast.projectile, barrel.position, baseRot, baseDir);
                }
                else
                {
                    // Eixo de rotação do spread = “right” relativo à direção base? não.
                    // Melhor: rotacionar em torno de um eixo ortogonal arbitrário para YAW puro,
                    // ou usar cone aleatório ao redor da direção base.
                    if (spellToCast.fixedSpread)
                    {
                        // Distribui uniformemente dentro do arco (yaw relativo ao próprio baseRot)
                        float total = spread;
                        for (int i = 0; i < shots; i++)
                        {
                            float t = (shots == 1) ? 0f : (i / (float)(shots - 1)) - 0.5f; // -0.5..0.5
                            float angle = t * total;
                            // gira ao redor do “up” relativo à direção base (mantém elevação)
                            Quaternion spreadRot = baseRot * Quaternion.AngleAxis(angle, Vector3.up);
                            Vector3 dir = spreadRot * Vector3.forward;
                            SpawnAndPush(spellToCast.projectile, barrel.position, spreadRot, dir);
                        }
                    }
                    else
                    {
                        // Cone aleatório ao redor da direção base (mais natural)
                        for (int i = 0; i < shots; i++)
                        {
                            float angle = Random.Range(-spread * 0.5f, spread * 0.5f);
                            Quaternion spreadRot = baseRot * Quaternion.AngleAxis(angle, Vector3.up);
                            Vector3 dir = spreadRot * Vector3.forward;
                            SpawnAndPush(spellToCast.projectile, barrel.position, spreadRot, dir);
                        }
                    }
                }
            }
            else
            {
                if (spellToCast.dudMuzzleParticles)
                    ObjectPoolingSystem.SpawnObject(spellToCast.dudMuzzleParticles, barrel.position, barrel.rotation);
            }
        }
    }

    private void SpawnAndPush(GameObject projectilePrefab, Vector3 pos, Quaternion rot, Vector3 dir)
    {
        var go = ObjectPoolingSystem.SpawnObject(projectilePrefab, pos, rot);

        // Força rotação/posição e forward do projétil (alguns pools mantêm rotação antiga)
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.forward = dir;

        // Aplica velocidade inicial SEM depender do forward anterior
        var proj = go.GetComponent<Projectile>();
        if (proj != null && proj.body != null)
        {
            proj.body.linearVelocity = dir.normalized * projectileSpeed;
            proj.body.angularVelocity = Vector3.zero;
        }
        else
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = dir.normalized * projectileSpeed;
        }
    }
}
