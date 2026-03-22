using UnityEngine;

public class CanvasPersister : MonoBehaviour
{
    public static CanvasPersister Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Torna o Canvas inteiro imortal!
    }
}