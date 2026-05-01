using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersister : MonoBehaviour
{
    public static EventSystemPersister Instance { get; private set; }

    void Awake()
    {
        // Regra Highlander: Só pode haver um!
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}