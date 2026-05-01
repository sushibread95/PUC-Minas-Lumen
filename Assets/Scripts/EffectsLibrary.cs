using UnityEngine;
[System.Serializable]
public struct Effect
{
    public enum EffectType { physical, magic, stamina };
    public EffectType effectType;
    public float power;
    public bool isPercentual;
}
public class EffectsLibrary : MonoBehaviour
{
    public Effect[] effects;
}