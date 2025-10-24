using UnityEngine;

public class OneShotProtection : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthSystem healthSystem;
    [Header("One Shot Protection Atributes")]
    [SerializeField][Range(0f, 1f)] private float protectionRange;
    [Header("Invincibility Frames Atributes")]
    [SerializeField] private float invincibilityFramesDuration;
    [Header("Adrenaline Rush Atributes")]
    [SerializeField] private float adrenalineDuration;
    [SerializeField] private float adrenalineResistance;
    [SerializeField] private bool needRevengeToSurvive; // if true then the player needs to kill an enemy in order to cleanse themselves of the adrenaline and not die. if false then nothings happends after adrenaline ends
    [SerializeField][Range(0f, 1f)] private float bossRevengeThreshold; // how much % of any Boss enemy healh needs to be dealt in order to count as revenge. This is supposed to be used alongside the  "needRevengeToSurvive" in order to make the OSP work on bosses. Set the value to 0 in case you dont want the player to be able to save themselves unless they kill
    [SerializeField][Range(0f, 1f)] private float revengeHealAmount; // how much health the player gets back on succesfull revenge. Cleansing themselves of the Adrenaline in the process
}
