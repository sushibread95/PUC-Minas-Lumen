using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponItem : ScriptableObject
{
    [Header("Visual")]
    public string weaponName;
    public GameObject weaponPrefab; // O modelo 3D da espada
    public Sprite icon;

    [Header("Combate")]
    public float baseDamage = 15f;
    public float attackRange = 1.5f;
    public float attackRate = 1.0f; // Tempo entre ataques

    [Header("Animação")]
    public string attackTrigger = "AttackMelee"; // Nome do Trigger no Animator
    public float damageWindowStart = 0.2f; // Em que momento da animação o dano liga (segundos)
    public float damageWindowEnd = 0.6f;   // Em que momento o dano desliga

    [Header("Efeitos (Opcional)")]
    // Caso queira que a espada gaste mana ou aplique status
    public List<ItemEffect> useEffects;
}