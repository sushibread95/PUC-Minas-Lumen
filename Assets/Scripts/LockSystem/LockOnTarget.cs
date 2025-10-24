using UnityEngine;

public class LockOnTarget : MonoBehaviour
{
    public Transform pivot;
    public float priority = 0f;
    public Transform Pivot => pivot ? pivot : transform;
}
