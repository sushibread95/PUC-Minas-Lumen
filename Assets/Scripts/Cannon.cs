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
    public int spellLayerIndex;
    public float castLockTime;

    [Header("Casting Atributes")]
    public float fireRate;
    public int magazine;
    public float cooldown;

    [Header("Mana Atributes")]
    public Effect[] castEffects; // O CUSTO para atirar

    [Header("Barrel Atributes")]
    public int projectilePerShot;
    public float spreadAngle;
    public bool fixedSpread;
    public float recoilForce;
}


public class Cannon : MonoBehaviour
{
    [Tooltip("Defina quem é o 'dono' deste canhão (Player ou Enemy)")]
    public ProjectileOwner ownerType = ProjectileOwner.None;

    // --- MUDANÇA: Referências separadas ---
    [Header("Components (Health)")]
    [Tooltip("Arraste o HealthSystem (do Player) aqui, SE for o Player")]
    [SerializeField] private HealthSystem playerHealth;
    [Tooltip("Arraste o EnemyHealth (do Inimigo) aqui, SE for um Inimigo")]
    [SerializeField] private EnemyHealth enemyHealth;
    // --- FIM DA MUDANÇA ---

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 25f;

    [Header("Lock-On Aim")]
    public LockOnSystem lockOn;
    public bool useLockAimWhenAvailable = true;
    public bool ignoreSpreadWhenLocked = false;

    [Header("Debug")]
    public bool debugDrawRay = false;
    public Color debugRayColor = new Color(1, 0.6f, 0.1f, 1);

    public void Fire(Spell spellToCast)
    {
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

        // --- MUDANÇA: Lógica de Custo Separada ---
        bool canCast = true;
        if (ownerType == ProjectileOwner.Player && playerHealth != null)
        {
            // Checa e aplica o custo no Player
            canCast = playerHealth.ApplyEffect(spellToCast.castEffects);
        }
        else if (ownerType == ProjectileOwner.Enemy && enemyHealth != null)
        {
            // Checa e aplica o custo no Inimigo (se ele tiver mana)
            canCast = enemyHealth.ApplyEffect(spellToCast.castEffects);
        }
        // --- FIM DA MUDANÇA ---


        for (int j = 0; j < spellToCast.cannonBarrels.Length; j++)
        {
            Transform barrel = spellToCast.cannonBarrels[j];
            if (!barrel) continue;

            if (canCast)
            {
                if (spellToCast.muzzleParticles)
                    ObjectPoolingSystem.SpawnObject(spellToCast.muzzleParticles, barrel.position, barrel.rotation);

                // ... (Lógica de mira) ...
                Vector3 aimPos;
                bool hasLockAim = useLockAimWhenAvailable && lockOn && lockOn.IsLockedOn && lockOn.CurrentAimPoint;
                if (hasLockAim) aimPos = lockOn.CurrentAimPoint.position;
                else aimPos = barrel.position + barrel.forward * 1000f;
                Vector3 baseDir = (aimPos - barrel.position);
                if (baseDir.sqrMagnitude < 0.0001f) baseDir = barrel.forward;
                baseDir.Normalize();
                Quaternion baseRot = Quaternion.LookRotation(baseDir, Vector3.up);
                if (debugDrawRay) Debug.DrawLine(barrel.position, barrel.position + baseDir * 30f, debugRayColor, 1.0f);

                // ... (Lógica de disparo) ...
                int shots = Mathf.Max(1, spellToCast.projectilePerShot);
                bool applySpread = !(ignoreSpreadWhenLocked && hasLockAim);
                float spread = applySpread ? Mathf.Max(0f, spellToCast.spreadAngle) : 0f;

                if (shots == 1 || spread <= 0f)
                {
                    SpawnAndPush(spellToCast.projectile, barrel.position, baseRot, baseDir);
                }
                else
                {
                    if (spellToCast.fixedSpread)
                    {
                        float total = spread;
                        for (int i = 0; i < shots; i++)
                        {
                            float t = (shots == 1) ? 0f : (i / (float)(shots - 1)) - 0.5f;
                            float angle = t * total;
                            Quaternion spreadRot = baseRot * Quaternion.AngleAxis(angle, Vector3.up);
                            Vector3 dir = spreadRot * Vector3.forward;
                            SpawnAndPush(spellToCast.projectile, barrel.position, spreadRot, dir);
                        }
                    }
                    else
                    {
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
        int projectileLayer = go.layer;
        int ownerLayer = this.gameObject.layer;

        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.forward = dir;

        var proj = go.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.owner = this.ownerType;
            proj.ignoredLayer = ownerLayer;
        }

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