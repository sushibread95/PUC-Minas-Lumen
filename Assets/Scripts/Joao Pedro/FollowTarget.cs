using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float speed;
    private void FixedUpdate()
    {
        Vector3 spdX = Vector3.Lerp(this.transform.position, target.position + offset - this.transform.position * 0.5f, speed * Time.fixedDeltaTime);
        this.transform.localPosition += spdX;
    }
}
