using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileKinetics : MonoBehaviour
{
    public float speed = 18f;
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void OnEnable()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
    }
}
