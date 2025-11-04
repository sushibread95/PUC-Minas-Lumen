using UnityEngine;
public class AutosaveTrigger : MonoBehaviour
{
    private bool hasBeenTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenTriggered)
        {
            hasBeenTriggered = true;

            Debug.LogWarning("AUTOSAVE DISPARADO!");

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }

            gameObject.SetActive(false);
        }
    }
}