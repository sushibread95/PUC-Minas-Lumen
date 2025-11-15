// PatrolState.cs
using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IEnemyState
{
    private readonly EnemyAIController controller;

    // Variáveis de runtime (privadas)
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float minWaypointDistance = 1f;
    private float waitTime = 3f; // Tempo que espera em um Waypoint

    public PatrolState(EnemyAIController controller)
    {
        this.controller = controller;
    }

    public void EnterState()
    {
        Debug.Log("IA: Entrou no estado de Patrulha.");
        controller.agent.isStopped = false;
        controller.agent.speed = controller.walkSpeed; // Lê a velocidade do Controller

        // Tenta achar o primeiro waypoint se houver
        if (controller.waypoints != null && controller.waypoints.Length > 0)
        {
            controller.agent.SetDestination(controller.waypoints[currentWaypointIndex].position); // Adicionamos .position
        }
    }

    public void UpdateState()
    {
        // 1. **VERIFICAÇÃO DE TRANSIÇÃO (Prioridade)**
        // Se viu ou ouviu, entra em Alerta.
        if (controller.CanSeeTarget() || controller.CanHearTarget())
        {
            controller.ChangeState(EnemyStateID.Alert);
            return;
        }

        // 2. **LÓGICA DE PATRULHA**
        // Se o inimigo não tiver waypoints, ele fica parado (lógica de guarda)
        if (controller.waypoints == null || controller.waypoints.Length == 0)
        {
            controller.transform.Rotate(0, 30 * Time.deltaTime, 0);
            return;
        }

        // Checa se chegou ao destino
        if (!controller.agent.pathPending && controller.agent.remainingDistance < minWaypointDistance)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % controller.waypoints.Length;
                controller.agent.SetDestination(controller.waypoints[currentWaypointIndex].position);
                waitTimer = 0f;
            }
        }
    }

    public void FixedUpdateState()
    {
    }

    public void ExitState()
    {
        controller.agent.isStopped = true;
        waitTimer = 0f;
    }
}