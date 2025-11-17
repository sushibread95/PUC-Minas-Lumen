using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public ProjectileOwner owner = ProjectileOwner.None;

    // --- 1. A LINHA QUE FALTAVA ---
    [HideInInspector] public int ignoredLayer = -1; // -1 significa "nenhuma"
    // --- FIM DA LINHA QUE FALTAVA ---

    public Rigidbody body;
    public float timeToLive = 5f;
    public float minSpeed = 0.1f;
    public ShrapnelSpawner[] shrapnelSpawner;

    private void OnEnable()
    {
        owner = ProjectileOwner.None;

        // --- 2. A LINHA QUE CAUSA O ERRO (Linha 20) ---
        ignoredLayer = -1; // Reseta a layer ignorada
        // --- FIM DA LINHA ---

        CancelInvoke();
        Invoke(nameof(Disable), timeToLive);
    }

    private void Disable()
    {
        gameObject.SetActive(false);
    }

    // 3D
    private void OnCollisionEnter(Collision collision)
    {
        // Spawn de shrapnel, se configurado
        if (shrapnelSpawner != null)
        {
            for (int i = 0; i < shrapnelSpawner.Length; i++)
            {
                var spawner = shrapnelSpawner[i];
                if (spawner.shrapnel == null || spawner.shrapnelCount <= 0) continue;

                for (int j = 0; j < spawner.shrapnelCount; j++)
                {
                    float angle = Random.Range(-spawner.spreadAngle * 0.5f, spawner.spreadAngle * 0.5f);
                    Quaternion spreadRot = Quaternion.Euler(0f, angle, 0f); // yaw
                    ObjectPoolingSystem.SpawnObject(spawner.shrapnel, transform.position, spreadRot * transform.rotation);
                }
            }
        }

        // Desativar projétil após impacto
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (body != null && body.linearVelocity.magnitude < minSpeed)
        {
            gameObject.SetActive(false);
        }
    }
}

[System.Serializable]
public struct ShrapnelSpawner
{
    public GameObject shrapnel;
    public int shrapnelCount;
    public float spreadAngle;
}
