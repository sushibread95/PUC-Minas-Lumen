using UnityEngine;

public enum Team { Player, Enemy, Neutral }

public class DamagePacket
{
    public float amount;
    public GameObject attacker;
    public Team team;
    public Effect[] effects;

    public DamagePacket(float amt, GameObject owner, Team t, Effect[] effs = null)
    {
        amount = amt;
        attacker = owner;
        team = t;
        effects = effs;
    }
}