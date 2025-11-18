using UnityEngine;

public class EventSystemPersister : MonoBehaviour
{
    void Awake()
    {
        // Certifique-se de que não estamos duplicando o EventSystem
        if (FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}