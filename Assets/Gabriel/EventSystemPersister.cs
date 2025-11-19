using UnityEngine;

public class EventSystemPersister : MonoBehaviour
{
    void Awake()
    {
        if (Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}