using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] private Transform target;
    void Update()
    {
        this.transform.rotation = target.rotation;
    }
}
