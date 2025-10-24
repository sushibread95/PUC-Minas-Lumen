using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class ObjectPoolingSystem : MonoBehaviour
{
    [SerializeField] private bool addDontDestroyOnLoad = false;
    private GameObject emptyHolder;
    private static GameObject gameObjectsEmpty;
    private static GameObject particleSystemsEmpty;
    private static GameObject soundFXEmpty;
    private static Dictionary<GameObject, ObjectPool<GameObject>> objectPool;
    private static Dictionary<GameObject, GameObject> cloneToPrefabMap;
    [HideInInspector] public enum PoolType { GameObjects, ParticleSystems, SoundFX }
    [SerializeField] private static PoolType poolingType;
    private void Awake()
    {
        objectPool = new Dictionary<GameObject, ObjectPool<GameObject>>();
        cloneToPrefabMap = new Dictionary<GameObject, GameObject>();

        SetupEmpties();
    }
    private void SetupEmpties()
    {
        emptyHolder = new GameObject("Object Pools");

        gameObjectsEmpty = new GameObject("Game Objects");
        gameObjectsEmpty.transform.SetParent(emptyHolder.transform);

        particleSystemsEmpty = new GameObject("Particle Effects");
        particleSystemsEmpty.transform.SetParent(emptyHolder.transform);

        soundFXEmpty = new GameObject("Sound Effects");
        soundFXEmpty.transform.SetParent(emptyHolder.transform);

        if (addDontDestroyOnLoad) DontDestroyOnLoad(soundFXEmpty.transform.root);
    }
    private static void CreatePool(GameObject prefab, Vector3 position, Quaternion rotation, PoolType poolType = PoolType.GameObjects)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>
        (
            createFunc: () => CreateObject(prefab, position, rotation, poolType),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroy
        );

        objectPool.Add(prefab, pool);
    }
    private static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, PoolType poolType = PoolType.GameObjects)
    {
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab, position, rotation);
        prefab.SetActive(true);
        GameObject parentObject = SetParentObject(poolType);
        obj.transform.SetParent(parentObject.transform);
        return obj;
    }
    private static void OnGetObject(GameObject obj)
    {
        
    }
    private static void OnReleaseObject(GameObject obj)
    {
        obj.SetActive(false);
    }
    private static void OnDestroy(GameObject obj)
    {
        if (cloneToPrefabMap.ContainsKey(obj)) cloneToPrefabMap.Remove(obj);
    }
    private static GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.GameObjects:
                return gameObjectsEmpty;
            case PoolType.ParticleSystems:
                return particleSystemsEmpty;
            case PoolType.SoundFX:
                return soundFXEmpty;
            default:
                return null;
        }
    }
    private static T SpawnObject<T>(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects) where T : Object
    {
        if (!objectPool.ContainsKey(objectToSpawn)) CreatePool(objectToSpawn, spawnPosition, spawnRotation, poolType);
        GameObject obj = objectPool[objectToSpawn].Get();
        if (obj != null)
        {
            if (!cloneToPrefabMap.ContainsKey(obj)) cloneToPrefabMap.Add(obj, objectToSpawn);

            obj.transform.position = spawnPosition;
            obj.transform.rotation = spawnRotation;;
            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject)) return obj as T;
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Object {objectToSpawn.name} doesn't have component of type {typeof(T)}");
                return null;
            }
            return component;
        }
        return null;
    }
    public static T SpawnObject<T>(T typePrefab, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects) where T : Component
    {
        return SpawnObject<T>(typePrefab.gameObject, spawnPosition, spawnRotation, poolType);
    }
    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects)
    {
        return SpawnObject<GameObject>(objectToSpawn, spawnPosition, spawnRotation, poolType);
    }
    public static void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObjects)
    {
        if (cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObject = SetParentObject(poolType);
            if (obj.transform.parent != parentObject.transform) obj.transform.SetParent(parentObject.transform);
            if (objectPool.TryGetValue(prefab, out ObjectPool<GameObject> pool)) pool.Release(obj);

        }
        else Debug.LogWarning($"Trying to return an object that is not pooled: {obj.name}");
    }
}
