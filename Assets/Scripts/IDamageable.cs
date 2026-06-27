// Contrato único para qualquer coisa que pode receber dano via Effect[].
// Implementado por EnemyHealth (inimigos) e HealthSystem (player).
// Substitui as chamadas por reflection (ApplyEffect/TakeDamage/...) por uma
// dispatch tipada e segura.
public interface IDamageable
{
    // Aplica os efeitos de dano/cura. Retorna true se o alvo processou o hit.
    bool ApplyEffect(Effect[] effects);
}
