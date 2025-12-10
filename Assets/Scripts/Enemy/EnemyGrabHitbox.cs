using UnityEngine;

public class EnemyGrabHitbox : MonoBehaviour
{
    private EnemyGrabber grabberScript;
    private Collider myCollider;

    void Awake()
    {
        // Tenta achar o script principal no pai ou na raiz
        grabberScript = GetComponentInParent<EnemyGrabber>();
        myCollider = GetComponent<Collider>();
        
        myCollider.isTrigger = true;
        // --- SEGREDO: FÍSICA SEMPRE LIGADA ---
        myCollider.enabled = true; 
    }

    // Não precisamos mais de Enable/Disable aqui, o colisor fica sempre on.

    void OnTriggerEnter(Collider other)
    {
        // Se bateu no player, avisa o Grabber.
        // O Grabber que decida se aceita ou ignora baseado no tempo.
        if (other.CompareTag("Player"))
        {
            PlayerControllerSystem player = other.GetComponent<PlayerControllerSystem>();
            if (player != null && grabberScript != null)
            {
                grabberScript.OnHitPlayer(player);
            }
        }
    }
}