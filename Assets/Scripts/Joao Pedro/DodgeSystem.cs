using System;
using UnityEngine;

public class DodgeSystem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Movement movement;
    [SerializeField] private HealthSystem healthSystem;
	[SerializeField] private ParticleSystem dodgeTrails;
    [Header("Dodge Atributes")]
    [SerializeField] private float force;
    [SerializeField] private float duration;
    [SerializeField] private float cooldown;
    [SerializeField][Range(0f, 1f)] private float resistance;
    private enum DodgeType {dash, teleport, parry}
    [SerializeField] private DodgeType dodgeType;
    private bool canDodge = true;
    private Vector2 dodgeForce;
    private float timer = 0f, cooldownTimer = 0f;
    private ParticleSystem.EmissionModule emd;
    private void Start() => emd = dodgeTrails.emission;
    private void Update()
    {
        if (canDodge)
        {
            Vector2 dodgeDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            dodgeForce = new Vector2(dodgeDirection.x * force, dodgeDirection.y * force);
            if (dodgeDirection != Vector2.zero)
            {
                //movement.body.angularVelocity = 0f;
                //movement.isDodging = true;
                canDodge = false;
            }
        }
        else
        {
            if (timer <= duration) timer += Time.deltaTime;
            else
            {
                //movement.isDodging = false;
                if (cooldownTimer <= cooldown) cooldownTimer += Time.deltaTime;
                else
                {
                    timer = cooldownTimer = 0f;
                    canDodge = true;
                }
            }
        }

        //movement.canMove = !movement.isDodging;
        //emd.enabled = movement.isDodging;
    }
    private void FixedUpdate()
    {
        // if (movement.isDodging)
        // {
        //     movement.body.linearVelocityX = dodgeForce.x;
        //     movement.body.linearVelocityY = dodgeForce.y;
        // }
    }
}
