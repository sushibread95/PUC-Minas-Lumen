using UnityEngine;
using System.Collections.Generic;

public class UniversalHitbox : MonoBehaviour
{
    public Team myTeam;
    public float damage;
    public Effect[] effects;
    
    private bool isActive = false;
    private List<Collider> alreadyHit = new List<Collider>();

    // Chamado pelas animações ou scripts de ataque
    public void Activate(float customDamage = -1)
    {
        if (customDamage > 0) damage = customDamage;
        isActive = true;
        alreadyHit.Clear();
    }

    public void Deactivate()
    {
        isActive = false;
        alreadyHit.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (alreadyHit.Contains(other)) return; // Impede dano múltiplo no mesmo frame

        UniversalHurtbox victim = other.GetComponent<UniversalHurtbox>();
        if (victim != null)
        {
            DamagePacket packet = new DamagePacket(damage, transform.root.gameObject, myTeam, effects);
            victim.TakeHit(packet);
            alreadyHit.Add(other);
        }
    }
}