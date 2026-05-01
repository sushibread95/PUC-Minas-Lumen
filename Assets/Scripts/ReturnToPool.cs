using UnityEngine;

public class ReturnToPool : MonoBehaviour
{
    [SerializeField] private GameObject objectToReturn;
    public void OnParticleSystemStopped() => ObjectPoolingSystem.ReturnObjectToPool(objectToReturn, ObjectPoolingSystem.PoolType.ParticleSystems);
}
