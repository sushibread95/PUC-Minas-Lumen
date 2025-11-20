using UnityEngine;

public class IADebugger : MonoBehaviour
{
    private EnemyAIController ai;

    void Start()
    {
        ai = GetComponent<EnemyAIController>();
    }

    void Update()
    {
        if (ai == null || ai.playerTarget == null) return;

        // Dados brutos
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = ai.playerTarget.position + Vector3.up * 1f;
        float dist = Vector3.Distance(eyePos, targetPos);
        
        Vector3 dirToPlayer = (targetPos - eyePos).normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);
        float angleThreshold = Mathf.Cos(ai.viewAngle * 0.5f * Mathf.Deg2Rad);
        bool angleCheck = dot >= angleThreshold;

        // Teste de Raycast manual
        bool raycastHitSomething = Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, ai.sightRange, ai.obstructionMask);

        // O Veredito
        if (dist > ai.sightRange)
        {
            Debug.Log($"<color=yellow>FALHA: Distância</color> | Dist: {dist:F1} / Max: {ai.sightRange}");
        }
        else if (!angleCheck)
        {
            Debug.Log($"<color=yellow>FALHA: Ângulo</color> | Dot: {dot:F2} / Precisa: {angleThreshold:F2} (Inimigo olhando para o lado errado?)");
        }
        else if (raycastHitSomething)
        {
            Debug.Log($"<color=red>FALHA: Obstrução</color> | Raio bateu em: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }
        else
        {
            Debug.Log($"<color=green>SUCESSO: Deveria estar vendo!</color> | Dist: {dist:F1} | Angle: OK | Ray: Livre");
            
            // Se der SUCESSO aqui, o problema é no PatrolState que não está chamando a troca
            if (ai.currentStateID == EnemyStateID.Patrol)
            {
                 Debug.LogError("ALERTA: A IA vê o player mas continua em PATROL. Verifique o PatrolState.cs!");
            }
        }

        // Desenha a linha para confirmar visualmente
        Debug.DrawLine(eyePos, targetPos, raycastHitSomething ? Color.red : Color.green);
    }
}